using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.Escalations;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using AlertHub.Infrastructure;
using AlertHub.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Tests;

public class EscalationScannerTests
{
    [Fact]
    public async Task A_pass_escalates_incidents_belonging_to_different_tenants()
    {
        using var host = new TestHost();

        var summary = await host.RunPassAsync();

        Assert.Equal(2, summary.Considered);
        Assert.Equal(2, summary.Notified + summary.ReachedNobody);

        // Each row must carry the tenant of its own incident, which only holds if every
        // incident was processed in a scope assigned to that tenant.
        var rows = host.AllEscalations();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.TenantId == host.AcmeTenantId);
        Assert.Contains(rows, r => r.TenantId == host.GlobexTenantId);
    }

    // Without IgnoreQueryFilters on the scan, a background scope has no tenant, the filter
    // collapses to Guid.Empty, and this returns nothing at all — the silent no-op.
    [Fact]
    public async Task The_scan_sees_work_despite_having_no_tenant_of_its_own()
    {
        using var host = new TestHost();

        var summary = await host.RunPassAsync();

        Assert.True(summary.Considered > 0);
        Assert.True(summary.HasNews);
    }

    [Fact]
    public async Task Acknowledged_incidents_drop_out_of_the_pass()
    {
        using var host = new TestHost();
        host.AcknowledgeAcmeIncident();

        var summary = await host.RunPassAsync();

        Assert.Equal(1, summary.Considered);
        Assert.Single(host.AllEscalations());
    }

    [Fact]
    public async Task Incidents_on_services_without_a_policy_drop_out_of_the_pass()
    {
        using var host = new TestHost();
        host.AddIncidentOnUnmanagedService();

        var summary = await host.RunPassAsync();

        // The extra incident is filtered by the scan, not merely skipped by the escalator.
        Assert.Equal(2, summary.Considered);
    }

    [Fact]
    public async Task Exhausted_incidents_drop_out_of_the_pass()
    {
        using var host = new TestHost();

        var first = await host.RunPassAsync();
        Assert.Equal(2, first.Considered);

        // Both incidents through all three tiers, then one pass past the last.
        for (var i = 1; i <= 3; i++)
            await host.RunPassAsync(minutesFromNow: i * 60);

        var afterExhaustion = await host.RunPassAsync(minutesFromNow: 500);

        // The scan's WHERE clause excludes them outright, so nothing is even considered.
        // The zero only means that next to the two above.
        Assert.Equal(0, afterExhaustion.Considered);
        Assert.False(afterExhaustion.HasNews);
    }

    // One bad incident must not stop every other tenant's escalations.
    [Fact]
    public async Task A_failure_on_one_incident_does_not_stop_the_others()
    {
        using var host = new TestHost();
        host.Sender.FailForIncident = host.AcmeIncidentId;

        var summary = await host.RunPassAsync();

        Assert.Equal(2, summary.Considered);
        Assert.Equal(1, summary.Failed);
        Assert.Equal(1, summary.Notified + summary.ReachedNobody);

        // Globex still got its page even though Acme threw.
        Assert.Contains(host.Sender.Sent, n => n.IncidentId == host.GlobexIncidentId);
    }

    [Fact]
    public async Task A_quiet_pass_reports_no_news()
    {
        using var host = new TestHost();
        await host.RunPassAsync();

        var second = await host.RunPassAsync();

        Assert.False(second.HasNews);
        Assert.Equal(2, second.NotYetDue);
    }

    /// <summary>
    /// Asserted separately because TestHost registers the escalation services by hand, so
    /// these tests would otherwise pass while the real composition root was missing them.
    /// </summary>
    [Fact]
    public void The_real_composition_root_registers_the_escalation_services()
    {
        var descriptors = TenantContextRegistrationTests.InfrastructureDescriptors();

        Assert.Contains(descriptors, d => d.ServiceType == typeof(INotificationSender));
        Assert.Contains(descriptors, d => d.ServiceType == typeof(IIncidentEscalator));
        Assert.Contains(descriptors, d => d.ServiceType == typeof(EscalationScanner));
        Assert.Contains(descriptors, d => d.ImplementationType?.Name == "EscalationWorker");
    }
}

/// <summary>
/// A container over a shared in-memory SQLite database, so the scanner's own scope handling
/// is exercised rather than stubbed. Two tenants, each with a policy and a live incident.
/// </summary>
public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _services;

    public FlakySender Sender { get; } = new();

    public Guid AcmeTenantId { get; }
    public Guid GlobexTenantId { get; }
    public Guid AcmeIncidentId { get; }
    public Guid GlobexIncidentId { get; }
    public Guid AcmeServiceId { get; }

    public TestHost()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Warning));
        services.AddHttpContextAccessor();
        services.AddDbContext<DbContext, AlertHubDbContext>(o => o.UseSqlite(_connection));

        services.AddScoped<ScopedTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ScopedTenantContext>());
        services.AddScoped<ITenantScope>(sp => sp.GetRequiredService<ScopedTenantContext>());

        services.AddScoped<IOnCallResolver, OnCallResolver>();
        services.AddScoped<IEscalationTargetResolver, EscalationTargetResolver>();
        services.AddScoped<IIncidentEscalator, IncidentEscalator>();
        services.AddScoped<EscalationScanner>();
        services.AddSingleton<INotificationSender>(Sender);
        services.AddSingleton(Options.Create(new EscalationSettings { MaxSendAttempts = 3 }));

        _services = services.BuildServiceProvider();

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();
        db.Database.EnsureCreated();

        (AcmeTenantId, AcmeServiceId, AcmeIncidentId) = SeedTenant(db, "Acme", "Payments API");
        (GlobexTenantId, _, GlobexIncidentId) = SeedTenant(db, "Globex", "Billing");
    }

    private static (Guid TenantId, Guid ServiceId, Guid IncidentId) SeedTenant(
        DbContext db, string tenantName, string serviceName)
    {
        var tenant = Tenant.Create(tenantName);
        var service = Service.Create(tenant.Id, serviceName);
        var responder = User.Create(
            tenant.Id, $"responder@{tenantName}.test", $"{tenantName} Responder", "hash",
            UserRole.Responder);

        var policy = EscalationPolicy.Create(tenant.Id, $"{tenantName} Critical");
        var tier1 = policy.AddTier(1, 5);
        policy.AddTier(2, 10);
        policy.AddTier(3, 20);
        service.SetEscalationPolicy(policy.Id);

        var incident = Incident.Create(
            tenant.Id, service.Id, $"{serviceName} is failing", IncidentSeverity.P1_Critical);

        db.AddRange(tenant, service, responder, policy, incident);
        db.Add(EscalationTierAssignment.ForRole(tier1.Id, tenant.Id, UserRole.Responder));
        db.SaveChanges();

        return (tenant.Id, service.Id, incident.Id);
    }

    public async Task<EscalationPassSummary> RunPassAsync(int minutesFromNow = 0)
    {
        using var scope = _services.CreateScope();
        var scanner = scope.ServiceProvider.GetRequiredService<EscalationScanner>();

        return await scanner.RunAsync(
            DateTime.UtcNow.AddMinutes(minutesFromNow), CancellationToken.None);
    }

    public List<IncidentEscalation> AllEscalations()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        return db.Set<IncidentEscalation>().IgnoreQueryFilters().ToList();
    }

    public void AcknowledgeAcmeIncident()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        var incident = db.Set<Incident>().IgnoreQueryFilters()
            .Single(i => i.Id == AcmeIncidentId);
        var responder = db.Set<User>().IgnoreQueryFilters()
            .First(u => u.TenantId == AcmeTenantId);

        incident.Acknowledge(responder.Id, responder.DisplayName);
        db.SaveChanges();
    }

    public void AddIncidentOnUnmanagedService()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DbContext>();

        var unmanaged = Service.Create(AcmeTenantId, "Docs site");
        db.Add(unmanaged);
        db.Add(Incident.Create(
            AcmeTenantId, unmanaged.Id, "Docs down", IncidentSeverity.P4_Low));
        db.SaveChanges();
    }

    public void Dispose()
    {
        _services.Dispose();
        _connection.Dispose();
    }
}

public sealed class FlakySender : INotificationSender
{
    public List<EscalationNotification> Sent { get; } = [];
    public Guid? FailForIncident { get; set; }

    public Task SendAsync(EscalationNotification notification, CancellationToken ct)
    {
        if (FailForIncident == notification.IncidentId)
            throw new InvalidOperationException("transport unavailable");

        Sent.Add(notification);
        return Task.CompletedTask;
    }
}
