DECLARE @TenantId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';
DECLARE @UserId UNIQUEIDENTIFIER = '999BA7A2-E5B4-40B7-A942-BEFBEB1C3E6C'; -- ymr (Admin)
DECLARE @ResponderId UNIQUEIDENTIFIER = 'CB229FD4-6A6E-4D9E-9764-72660227DCC2'; -- test-responder

-- ============ SERVICES ============
DECLARE @SvcApi UNIQUEIDENTIFIER = NEWID();
DECLARE @SvcAuth UNIQUEIDENTIFIER = NEWID();
DECLARE @SvcPayments UNIQUEIDENTIFIER = NEWID();
DECLARE @SvcNotif UNIQUEIDENTIFIER = NEWID();

INSERT INTO Services (Id, TenantId, Name, Description, CreatedAt, EscalationPolicyId) VALUES
(@SvcApi,      @TenantId, 'API Gateway',           'Main API gateway and load balancer',           DATEADD(DAY, -30, GETUTCDATE()), NULL),
(@SvcAuth,     @TenantId, 'Auth Service',           'Authentication and authorization service',     DATEADD(DAY, -28, GETUTCDATE()), NULL),
(@SvcPayments, @TenantId, 'Payment Processing',     'Stripe integration and payment workflows',     DATEADD(DAY, -25, GETUTCDATE()), NULL),
(@SvcNotif,    @TenantId, 'Notification Service',   'Email, SMS, and push notification delivery',   DATEADD(DAY, -20, GETUTCDATE()), NULL);

-- ============ INCIDENTS ============

-- 1) Triggered (fresh, no action yet)
DECLARE @Inc1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc1, @TenantId, @SvcApi, 'High latency on /api/v2/orders endpoint', 'P2_High', 'Triggered',
    DATEADD(MINUTE, -15, GETUTCDATE()), '{"source":"datadog","metric":"p99_latency_ms","value":2400}', NULL, NULL, NULL);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt)
VALUES (NEWID(), @Inc1, @TenantId, 'Created', 'Incident triggered', DATEADD(MINUTE, -15, GETUTCDATE()));

-- 2) Triggered (critical)
DECLARE @Inc2 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc2, @TenantId, @SvcPayments, 'Payment webhook failures spiking', 'P1_Critical', 'Triggered',
    DATEADD(MINUTE, -5, GETUTCDATE()), '{"source":"stripe","error_rate":"12%"}', NULL, NULL, NULL);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt)
VALUES (NEWID(), @Inc2, @TenantId, 'Created', 'Incident triggered', DATEADD(MINUTE, -5, GETUTCDATE()));

-- 3) Acknowledged
DECLARE @Inc3 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc3, @TenantId, @SvcAuth, 'JWT token validation errors in production', 'P2_High', 'Acknowledged',
    DATEADD(HOUR, -2, GETUTCDATE()), NULL, DATEADD(HOUR, -1, GETUTCDATE()), NULL, @UserId);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt) VALUES
(NEWID(), @Inc3, @TenantId, 'Created', 'Incident triggered', DATEADD(HOUR, -2, GETUTCDATE())),
(NEWID(), @Inc3, @TenantId, 'Acknowledged', 'Acknowledged by ymr', DATEADD(HOUR, -1, GETUTCDATE()));

-- 4) Investigating
DECLARE @Inc4 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc4, @TenantId, @SvcNotif, 'SMS delivery rate dropped below 80%', 'P3_Medium', 'Investigating',
    DATEADD(HOUR, -6, GETUTCDATE()), '{"source":"twilio","delivery_rate":0.72}', DATEADD(HOUR, -5, GETUTCDATE()), NULL, @ResponderId);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt) VALUES
(NEWID(), @Inc4, @TenantId, 'Created', 'Incident triggered', DATEADD(HOUR, -6, GETUTCDATE())),
(NEWID(), @Inc4, @TenantId, 'Acknowledged', 'Acknowledged by test-responder', DATEADD(HOUR, -5, GETUTCDATE())),
(NEWID(), @Inc4, @TenantId, 'InvestigationStarted', 'Investigation started', DATEADD(HOUR, -4, GETUTCDATE()));

-- 5) Resolved (recent)
DECLARE @Inc5 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc5, @TenantId, @SvcApi, 'Memory leak causing OOM restarts', 'P1_Critical', 'Resolved',
    DATEADD(DAY, -2, GETUTCDATE()), NULL,
    DATEADD(HOUR, -47, GETUTCDATE()),
    DATEADD(HOUR, -44, GETUTCDATE()), @UserId);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt) VALUES
(NEWID(), @Inc5, @TenantId, 'Created', 'Incident triggered', DATEADD(DAY, -2, GETUTCDATE())),
(NEWID(), @Inc5, @TenantId, 'Acknowledged', 'Acknowledged by ymr', DATEADD(HOUR, -47, GETUTCDATE())),
(NEWID(), @Inc5, @TenantId, 'InvestigationStarted', 'Investigation started', DATEADD(HOUR, -46, GETUTCDATE())),
(NEWID(), @Inc5, @TenantId, 'Resolved', 'Fixed memory leak in connection pool. Deployed hotfix v2.4.1.', DATEADD(HOUR, -44, GETUTCDATE()));

-- 6) Resolved (older)
DECLARE @Inc6 UNIQUEIDENTIFIER = NEWID();
INSERT INTO Incidents (Id, TenantId, ServiceId, Title, Severity, Status, CreatedAt, RawAlertPayload, AcknowledgedAt, ResolvedAt, AcknowledgedByUserId)
VALUES (@Inc6, @TenantId, @SvcPayments, 'Duplicate charge events from Stripe', 'P2_High', 'Resolved',
    DATEADD(DAY, -7, GETUTCDATE()), NULL,
    DATEADD(HOUR, -167, GETUTCDATE()),
    DATEADD(HOUR, -165, GETUTCDATE()), @ResponderId);

INSERT INTO IncidentEvents (Id, IncidentId, TenantId, EventType, Description, OccurredAt) VALUES
(NEWID(), @Inc6, @TenantId, 'Created', 'Incident triggered', DATEADD(DAY, -7, GETUTCDATE())),
(NEWID(), @Inc6, @TenantId, 'Acknowledged', 'Acknowledged by test-responder', DATEADD(HOUR, -167, GETUTCDATE())),
(NEWID(), @Inc6, @TenantId, 'Resolved', 'Added idempotency key check to webhook handler. Refunded 3 affected customers.', DATEADD(HOUR, -165, GETUTCDATE()));

-- ============ ESCALATION POLICIES ============

-- Policy 1: Critical Infrastructure (3 tiers)
DECLARE @Policy1 UNIQUEIDENTIFIER = NEWID();
INSERT INTO EscalationPolicies (Id, TenantId, Name) VALUES (@Policy1, @TenantId, 'Critical Infrastructure');

DECLARE @Tier1_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Tier1_2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Tier1_3 UNIQUEIDENTIFIER = NEWID();

INSERT INTO EscalationTiers (Id, EscalationPolicyId, TenantId, [Level], EscalateAfterMinutes) VALUES
(@Tier1_1, @Policy1, @TenantId, 1, 5),
(@Tier1_2, @Policy1, @TenantId, 2, 15),
(@Tier1_3, @Policy1, @TenantId, 3, 30);

INSERT INTO EscalationTierAssignments (Id, EscalationTierId, TenantId, UserId, EscalationRole, UseOnCallSchedule) VALUES
-- Tier 1: notify all responders + on-call
(NEWID(), @Tier1_1, @TenantId, NULL,    'Responder', 0),
(NEWID(), @Tier1_1, @TenantId, NULL,    NULL,         1),
-- Tier 2: notify specific admin user
(NEWID(), @Tier1_2, @TenantId, @UserId, NULL,         0),
-- Tier 3: notify all admins
(NEWID(), @Tier1_3, @TenantId, NULL,    'Admin',      0);

-- Policy 2: Business Services (2 tiers)
DECLARE @Policy2 UNIQUEIDENTIFIER = NEWID();
INSERT INTO EscalationPolicies (Id, TenantId, Name) VALUES (@Policy2, @TenantId, 'Business Services');

DECLARE @Tier2_1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Tier2_2 UNIQUEIDENTIFIER = NEWID();

INSERT INTO EscalationTiers (Id, EscalationPolicyId, TenantId, [Level], EscalateAfterMinutes) VALUES
(@Tier2_1, @Policy2, @TenantId, 1, 10),
(@Tier2_2, @Policy2, @TenantId, 2, 30);

INSERT INTO EscalationTierAssignments (Id, EscalationTierId, TenantId, UserId, EscalationRole, UseOnCallSchedule) VALUES
-- Tier 1: notify specific responder
(NEWID(), @Tier2_1, @TenantId, @ResponderId, NULL, 0),
-- Tier 2: notify admin user
(NEWID(), @Tier2_2, @TenantId, @UserId,      NULL, 0);

-- Link policies to services
UPDATE Services SET EscalationPolicyId = @Policy1 WHERE Id IN (@SvcApi, @SvcAuth);
UPDATE Services SET EscalationPolicyId = @Policy2 WHERE Id IN (@SvcPayments, @SvcNotif);

-- ============ ON-CALL SHIFTS ============
-- Anchored to a daily 08:00 UTC handoff relative to today, so the roster always covers
-- "now" however long after seeding it is read. Without this, Critical Infrastructure
-- tier 1 carries a UseOnCallSchedule assignment that resolves to nobody.
--
-- These inserts bypass the application's overlap rule, so the windows below are written
-- to respect it: they touch at the handoff instant but never overlap. WindowEnd is
-- exclusive, so the 08:00 handoff belongs to the incoming shift.
DECLARE @Handoff DATETIME2 =
    DATEADD(HOUR, 8, CAST(CAST(DATEADD(DAY, -1, GETUTCDATE()) AS DATE) AS DATETIME2));

INSERT INTO OnCallShifts (Id, TenantId, ServiceId, UserId, WindowStart, WindowEnd, TimeZoneId, CreatedAt) VALUES
-- API Gateway: daily rotation with one deliberately unstaffed day, so a coverage gap is
-- visible in the roster and resolves to nobody rather than to a fallback.
(NEWID(), @TenantId, @SvcApi,  @UserId,      @Handoff,                  DATEADD(DAY, 1, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
(NEWID(), @TenantId, @SvcApi,  @ResponderId, DATEADD(DAY, 1, @Handoff), DATEADD(DAY, 2, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
(NEWID(), @TenantId, @SvcApi,  @UserId,      DATEADD(DAY, 2, @Handoff), DATEADD(DAY, 3, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
(NEWID(), @TenantId, @SvcApi,  @ResponderId, DATEADD(DAY, 3, @Handoff), DATEADD(DAY, 4, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
-- day +4 to +5 left uncovered on purpose
(NEWID(), @TenantId, @SvcApi,  @UserId,      DATEADD(DAY, 5, @Handoff), DATEADD(DAY, 6, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
-- Auth Service: continuous two-day rotation, no gaps.
(NEWID(), @TenantId, @SvcAuth, @ResponderId, @Handoff,                  DATEADD(DAY, 2, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
(NEWID(), @TenantId, @SvcAuth, @UserId,      DATEADD(DAY, 2, @Handoff), DATEADD(DAY, 4, @Handoff), 'Asia/Jerusalem', GETUTCDATE()),
(NEWID(), @TenantId, @SvcAuth, @ResponderId, DATEADD(DAY, 4, @Handoff), DATEADD(DAY, 6, @Handoff), 'Asia/Jerusalem', GETUTCDATE());

PRINT 'Seed completed successfully.';
