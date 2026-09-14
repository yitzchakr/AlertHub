using AlertHub.Application.Auth.Login;
using AlertHub.Application.Auth.Refresh;
using AlertHub.Application.Features.Incidents.CreateIncident;
using AlertHub.Application.Features.Users;
using AlertHub.Domain.Entities;

namespace AlertHub.Application.Tests;

/// <summary>
/// Records synthesize a ToString() that prints every property, and these four carry
/// credentials through a message pipeline that logs failures. A failed sign-in was enough
/// to write a plaintext password into the log file.
///
/// These assert on ToString() because that is the single hook every one of those paths goes
/// through — structured logging, string interpolation, exception messages.
/// </summary>
public class SecretRedactionTests
{
    private const string Secret = "Sup3rSecretPassword";
    private const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token";

    /// <summary>
    /// PrintMembers is matched by signature, not by an override keyword — there is no base
    /// method to override, so the compiler simply stops generating its own when it finds one
    /// that matches. Get the signature subtly wrong and it generates its own anyway, ours
    /// becomes dead code, and the secret is back in the log with nothing to say so.
    ///
    /// Asserting on the literal marker rather than only on the secret's absence is what
    /// catches that: the compiler's version would never emit "[redacted]".
    /// </summary>
    private const string Marker = "[redacted]";

    [Fact]
    public void A_login_command_does_not_print_the_password()
    {
        var rendered = new LoginCommand("dana@acme.test", Secret).ToString();

        Assert.DoesNotContain(Secret, rendered);
        Assert.Contains(Marker, rendered);
        // The account that failed is the reason the line exists, so it stays.
        Assert.Contains("dana@acme.test", rendered);
    }

    [Fact]
    public void A_create_user_command_does_not_print_the_password()
    {
        var rendered = new CreateUserCommand(
            "Dana Responder", Secret, "dana@acme.test",
            PermissionRole.Responder, EscalationRole.Responder).ToString();

        Assert.DoesNotContain(Secret, rendered);
        Assert.Contains(Marker, rendered);
        Assert.Contains("dana@acme.test", rendered);
        Assert.Contains("Responder", rendered);
    }

    [Fact]
    public void A_refresh_command_prints_neither_token()
    {
        var rendered = new RefreshCommand(Token, "refresh-token-value").ToString();

        Assert.DoesNotContain(Token, rendered);
        Assert.DoesNotContain("refresh-token-value", rendered);
        Assert.Contains(Marker, rendered);
    }

    [Fact]
    public void A_login_response_prints_neither_token()
    {
        var rendered = new LoginResponse(Token, "refresh-token-value").ToString();

        Assert.DoesNotContain(Token, rendered);
        Assert.DoesNotContain("refresh-token-value", rendered);
        Assert.Contains(Marker, rendered);
    }

    // Not a credential field, but an unbounded blob written by a third party: a webhook body
    // can carry the sender's own signing token, and dumping it whole also floods the log.
    [Fact]
    public void An_incident_command_prints_the_payload_size_rather_than_the_payload()
    {
        var payload = """{"source":"datadog","signing_token":"abc123"}""";

        var rendered = new CreateIncidentCommand(
            Guid.NewGuid(), "Checkout returning 500s", IncidentSeverity.P1_Critical, payload)
            .ToString();

        Assert.DoesNotContain("abc123", rendered);
        Assert.Contains($"<{payload.Length} chars>", rendered);
        // The parts that make the line worth reading survive.
        Assert.Contains("Checkout returning 500s", rendered);
        Assert.Contains("P1_Critical", rendered);
    }

    [Fact]
    public void An_incident_command_with_no_payload_says_so()
    {
        var rendered = new CreateIncidentCommand(
            Guid.NewGuid(), "Manual incident", IncidentSeverity.P3_Medium, null).ToString();

        Assert.Contains("RawPayload = null", rendered);
    }

    // Redaction must not cost the records their value semantics: Wolverine and the tests
    // both rely on equality, which a hand-written ToString() does not touch but a careless
    // conversion to a class would.
    [Fact]
    public void Redaction_leaves_record_equality_intact()
    {
        Assert.Equal(
            new LoginCommand("dana@acme.test", Secret),
            new LoginCommand("dana@acme.test", Secret));

        Assert.NotEqual(
            new LoginCommand("dana@acme.test", Secret),
            new LoginCommand("dana@acme.test", "different"));
    }
}
