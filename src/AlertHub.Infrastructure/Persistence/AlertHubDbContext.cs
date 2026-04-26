using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace AlertHub.Infrastructure.Persistence;

public class AlertHubDbContext(DbContextOptions<AlertHubDbContext> options, ITenantContext? tenantContext = null) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();
    public DbSet<EscalationPolicy> EscalationPolicies => Set<EscalationPolicy>();
    public DbSet<EscalationTier> EscalationTiers => Set<EscalationTier>();
    public DbSet<EscalationTierAssignment> EscalationTierAssignments => Set<EscalationTierAssignment>();
    public DbSet<OnCallSchedule> OnCallSchedules => Set<OnCallSchedule>();
    private readonly ITenantContext? _tenantContext = tenantContext;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlertHubDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filter = CreateTenantFilter(entityType.ClrType);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    private LambdaExpression CreateTenantFilter(Type entityType)
    {
        // Builds: e => _tenantContext != null && e.TenantId == _tenantContext.TenantId
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

        // Use Expression.Field — _tenantContext is a field, not a property.
        // Expression.Constant(this) captures the DbContext instance, but because
        // _tenantContext is a scoped service resolved per-request, EF re-evaluates
        // the field access each time the filter runs.
        var tenantContextField = Expression.Field(
            Expression.Constant(this), "_tenantContext");
        var currentTenantId = Expression.Property(tenantContextField, nameof(ITenantContext.TenantId));

        // Guard against null _tenantContext (e.g. during migrations / design-time)
        var nullCheck = Expression.NotEqual(
            tenantContextField,
            Expression.Constant(null, typeof(ITenantContext)));
        var tenantMatch = Expression.Equal(tenantIdProperty, currentTenantId);
        var filter = Expression.AndAlso(nullCheck, tenantMatch);

        return Expression.Lambda(filter, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantContext is not null)
        {
            var currentTenantId = _tenantContext.TenantId;
            EnforceTenantId(currentTenantId);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        if (_tenantContext is not null)
        {
            var currentTenantId = _tenantContext.TenantId;
            EnforceTenantId(currentTenantId);
        }

        return base.SaveChanges();
    }

    private void EnforceTenantId(Guid currentTenantId)
    {
        var tenantEntries = ChangeTracker.Entries<ITenantEntity>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entry in tenantEntries)
        {
            if (entry.Entity.TenantId != currentTenantId)
            {
                throw new InvalidOperationException(
                    $"Entity '{entry.Entity.GetType().Name}' has TenantId " +
                    $"'{entry.Entity.TenantId}' but the current tenant is '{currentTenantId}'.");
            }
        }
    }
}

