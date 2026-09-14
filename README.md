# AlertHub

An incident-management backend: services raise incidents, escalation policies decide who gets
paged and in what order, and a background worker walks each incident up its policy until
somebody acknowledges it or the tiers run out.

The interesting part is not the CRUD. It is what happens when nobody answers — how a page
escalates on a timer, how the same incident is not paged twice by two instances, how a tier
that resolves to nobody is made visible instead of silently succeeding, and how a policy that
has exhausted every tier stops paging rather than nagging forever.

- **Stack** — .NET 10, ASP.NET Core minimal APIs, Wolverine, EF Core 10, SQL Server, xUnit
- **Front-end** — [alerthub-frontend](https://github.com/yitzchakr/alerthub-frontend) (React 19 + TypeScript + Vite)

## Layout

| Project | Holds |
| --- | --- |
| `AlertHub.Domain` | Entities, invariants, domain events. No dependencies. |
| `AlertHub.Application` | One folder per use case: command/query, handler, validator, DTO. |
| `AlertHub.Infrastructure` | EF Core, migrations, JWT, SMTP, the escalation worker. |
| `AlertHub.Api` | Endpoint mapping, exception middleware, OpenAPI. |

Vertical slices rather than layers: `Features/OnCall/CreateOnCallShift/` contains everything
that request needs, so a feature is added by adding a folder, not by touching four of them.

## Running it

Requires the .NET 10 SDK and a SQL Server instance (LocalDB or Developer Edition both work).

**1. Supply a JWT signing key.** It is deliberately not in `appsettings.json`, so a fresh clone
has none and the app refuses to start until you set one:

```
dotnet user-secrets set "Jwt:SecretKey" "$(openssl rand -base64 64)" --project src/AlertHub.Api
```

Any string of at least 32 bytes works — HS256 rejects shorter keys. See
[Configuration](#configuration) for the non-development equivalent.

**2. Create the database.** The default connection string points at `localhost`; override
`ConnectionStrings:AlertHub` in user-secrets if yours differs.

```
dotnet ef database update --project src/AlertHub.Infrastructure --startup-project src/AlertHub.Api
```

**3. Run it.**

```
dotnet run --project src/AlertHub.Api
```

HTTP on `:5169`, HTTPS on `:7194`. In Development you also get an interactive API browser
at `/scalar/v1` and the raw document at `/openapi/v1.json`; both are gated to Development.

### Seed data

`seed.sql` creates four services, two escalation policies and a rolling on-call roster. It
assumes the two user rows named at the top of the file already exist, because **there is no
registration endpoint** — only `POST /api/auth/login` and `/refresh`. On a fresh database,
insert a user directly with a BCrypt hash in `PasswordHash` (the app hashes with
`BCryptPasswordHasher`), matching the GUIDs at the top of `seed.sql`, then run the script.

That first row needs both role columns — `PermissionRole` for what they may do and
`EscalationRole` for which sweep pages them (see [Two kinds of role](#two-kinds-of-role)) —
and `PermissionRole` must be `'Admin'` on at least one of them, since creating any further
user is an Admin-only endpoint.

The roster is anchored to a daily 08:00 UTC handoff relative to *today*, so it still covers
"now" whenever you get round to reading it.

## How escalation works

`EscalationWorker` ticks on a `PeriodicTimer` (30s by default). Each pass, `EscalationScanner`
finds incidents that are still `Triggered`, have a policy, and have a tier whose delay has
elapsed — then advances each one exactly one tier.

Three decisions worth knowing about:

**Idempotency comes from the database, not from locking.** `IncidentEscalations` has a unique
index on `(IncidentId, Level)`. Two instances racing the same tier both insert; one wins, the
loser catches the `DbUpdateException`, re-reads, and moves on. No distributed lock, no leader
election.

**Attempts are counted before the send, not after.** A crash midway through notifying leaves
the attempt recorded, so a persistently failing tier is abandoned after
`Escalation:MaxSendAttempts` instead of retrying forever.

**Exhaustion is recorded.** When the last tier has fired and nobody has acknowledged,
`Incident.EscalationExhaustedAt` is set and the incident stops being a candidate. Without
this, an incident whose entire on-call chain is on holiday would be paged indefinitely.

The worker logs a summary every pass — at `Information` when something happened, `Debug` when
nothing was due. The quiet line exists because a worker with nothing to do and a worker that
has died otherwise look identical.

Tenancy is enforced by a global query filter, so the scanner's cross-tenant sweep is the one
place in the codebase that calls `IgnoreQueryFilters()`. It then opens a tenant-scoped DI scope
per incident, and every read inside that scope is filtered normally.

## Email

`Email:Enabled` is `false` in development, which wires `LoggingNotificationSender` — it logs
each recipient's name and address rather than sending. To exercise the real MailKit path
without mailing anyone, run a local catcher:

```
dotnet tool install -g Rnwood.Smtp4dev
```

Then start it on port 1025 with its UI on 5001, set `Email:Enabled` to `true`, and point
`Email:RedirectAllTo` at any address — every message is rewritten to that one recipient before
it reaches the transport, so seeded addresses can never receive a real page. The redirect
decorator is wired unconditionally, so there is no safety to remember to switch on, and
`EmailModeAnnouncer` warns at boot when it is off.

## Configuration

| Key | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:AlertHub` | `localhost` / `AlertHub` | Trusted connection. |
| `Jwt:SecretKey` | *(none)* | Required. User-secrets in dev, `Jwt__SecretKey` elsewhere. |
| `Jwt:Issuer` / `Jwt:Audience` | `AlertHub` | Validated on every token. |
| `Jwt:AccessTokenExpirationMinutes` | `15` | |
| `Escalation:Enabled` | `true` | `false` stops the worker without removing it. |
| `Escalation:PollIntervalSeconds` | `30` | |
| `Escalation:MaxSendAttempts` | `3` | Per tier, then the tier is abandoned. |
| `Email:Enabled` | `false` | `false` logs instead of sending. |
| `Email:RedirectAllTo` | *(none)* | Rewrites every recipient to this address. |

Settings are validated at startup, not at first use — a missing signing key or a mistyped
redirect address stops the host with a message naming the setting, rather than surfacing on
the first sign-in or the first page.

## Tests

```
dotnet test
```

268 tests across three projects. Application tests run against SQLite in-memory with two
seeded tenants, so tenant isolation is asserted rather than assumed. Where a rule exists in
both C# and SQL — the half-open `[Start, End)` on-call window, for instance —
`OnCallShiftQueriesTests` asserts the two agree, because a filter that drifts from the
in-memory predicate is a bug no single-sided test would catch.

## Two kinds of role

A user carries two roles, and conflating them is a mistake this codebase made once.

| Column | Answers | Read by |
| --- | --- | --- |
| `PermissionRole` | What may this person do? | The `role` claim, every `RequireRole` |
| `EscalationRole` | Who does a role-targeting tier page? | `EscalationTargetResolver` |

They were one field. Granting somebody Admin so they could edit a policy also added them to
every tier that paged Admin, and demoting somebody could empty a tier nobody was watching —
both through a screen that said nothing about paging. The values still overlap, because the
migration copies the old column into the new one so no existing rota changes, but nothing
keeps them aligned from here.

## Leavers

There is no delete. `User.DeactivatedAt` records a departure, and the schema is the reason:
`EscalationTierAssignment.UserId` is `Restrict`, so anybody named on a tier could not be
deleted anyway, and `Incident.AcknowledgedByUserId` is `SetNull`, so a delete would quietly
rewrite answered incidents into unanswered ones.

Deactivating revokes the refresh token, drops the user out of role sweeps and direct tier
targets, and makes their on-call shifts resolve to nobody rather than to a name that will
never answer. An access token already issued still stands until it expires — at most
`Jwt:AccessTokenExpirationMinutes` — which is the standing cost of validating tokens without
a round trip.

A tier left reaching nobody is not an error here; it is the state `EscalationTargetResolver`
exists to make visible, and `GET /api/escalation-policies/{id}/tiers/{tierId}/targets`
answers it before an outage does.

## Notable design notes

- **Half-open time windows.** On-call shifts are `[Start, End)`, so a shift ending at 08:00
  hands over cleanly to one starting at 08:00 — they may touch, never overlap. The rule lives
  in `TimeWindow` and is mirrored in exactly one place in SQL, `OnCallShiftQueries`.
- **UTC by convention, not by discipline.** A value converter applied through
  `ConfigureConventions` stamps `DateTimeKind.Utc` on every `DateTime` read back from the
  database, so no handler has to remember.
- **Errors do not leak.** `ExceptionHandlingMiddleware` maps domain exceptions to RFC 9457
  ProblemDetails and returns only a trace id for unhandled ones outside Development.
