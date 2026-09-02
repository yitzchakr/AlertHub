using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using AlertHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Application.Tests;

/// <summary>
/// A real relational database per test, on SQLite in memory. EF's in-memory provider is
/// deliberately not used: the behaviour under test includes the global tenant query
/// filter, the SaveChanges tenant guard, and predicates that must translate to SQL, none
/// of which a non-relational provider exercises honestly.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MutableTenantContext _tenantContext = new();

    public AlertHubDbContext Db { get; }

    public Guid TenantId { get; }
    public Guid OtherTenantId { get; }
    public Guid ServiceId { get; }
    public Guid SecondServiceId { get; }
    public Guid OtherTenantServiceId { get; }
    public Guid AdminId { get; }
    public Guid ResponderId { get; }
    public Guid SecondResponderId { get; }

    /// <summary>Tier assigned to the on-call schedule and to the Responder role.</summary>
    public Guid MixedTierId { get; }

    /// <summary>Tier assigned only to the on-call schedule.</summary>
    public Guid OnCallOnlyTierId { get; }

    /// <summary>Tier naming the admin directly, who is also caught by no role sweep.</summary>
    public Guid DirectUserTierId { get; }

    public Guid IncidentId { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AlertHubDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new AlertHubDbContext(options, _tenantContext);
        Db.Database.EnsureCreated();

        // Seeding runs with no tenant in scope, which switches off the SaveChanges guard
        // so rows for two tenants can be created in one pass. Reads are impossible while
        // unscoped, which is why every assertion sets a tenant first.
        var tenant = Tenant.Create("Acme");
        var otherTenant = Tenant.Create("Globex");
        TenantId = tenant.Id;
        OtherTenantId = otherTenant.Id;

        var service = Service.Create(TenantId, "Payments API");
        var secondService = Service.Create(TenantId, "Auth Service");
        var otherService = Service.Create(OtherTenantId, "Globex Billing");
        ServiceId = service.Id;
        SecondServiceId = secondService.Id;
        OtherTenantServiceId = otherService.Id;

        var admin = User.Create(TenantId, "admin@acme.test", "Avery Admin", "hash", UserRole.Admin);
        var responder = User.Create(TenantId, "dana@acme.test", "Dana Responder", "hash", UserRole.Responder);
        var secondResponder = User.Create(TenantId, "eli@acme.test", "Eli Responder", "hash", UserRole.Responder);
        var otherTenantResponder = User.Create(
            OtherTenantId, "zoe@globex.test", "Zoe Outsider", "hash", UserRole.Responder);
        AdminId = admin.Id;
        ResponderId = responder.Id;
        SecondResponderId = secondResponder.Id;

        var policy = EscalationPolicy.Create(TenantId, "Payments Critical");
        var mixedTier = policy.AddTier(1, 5);
        var onCallOnlyTier = policy.AddTier(2, 10);
        var directUserTier = policy.AddTier(3, 20);
        MixedTierId = mixedTier.Id;
        OnCallOnlyTierId = onCallOnlyTier.Id;
        DirectUserTierId = directUserTier.Id;

        var incident = Incident.Create(
            TenantId, ServiceId, "Checkout returning 500s", IncidentSeverity.P1_Critical);
        IncidentId = incident.Id;

        Db.AddRange(tenant, otherTenant);
        Db.AddRange(service, secondService, otherService);
        Db.AddRange(admin, responder, secondResponder, otherTenantResponder);
        Db.Add(policy);
        Db.Add(incident);
        Db.AddRange(
            EscalationTierAssignment.ForOnCall(MixedTierId, TenantId),
            EscalationTierAssignment.ForRole(MixedTierId, TenantId, UserRole.Responder),
            EscalationTierAssignment.ForOnCall(OnCallOnlyTierId, TenantId),
            EscalationTierAssignment.ForUser(DirectUserTierId, TenantId, AdminId));
        Db.SaveChanges();

        AsTenant(TenantId);
    }

    /// <summary>Scopes subsequent queries and saves to a tenant, as a request would.</summary>
    public void AsTenant(Guid? tenantId) => _tenantContext.TenantId = tenantId;

    public ITenantContext TenantContext => _tenantContext;

    public OnCallShift AddShift(Guid serviceId, Guid userId, DateTime startUtc, DateTime endUtc)
    {
        var shift = OnCallShift.Create(
            TenantId, serviceId, userId, TimeWindow.Create(startUtc, endUtc));

        Db.Add(shift);
        Db.SaveChanges();
        return shift;
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }

    private sealed class MutableTenantContext : ITenantContext
    {
        public Guid? TenantId { get; set; }
    }
}
