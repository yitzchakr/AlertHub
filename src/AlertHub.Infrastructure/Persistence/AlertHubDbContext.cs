using AlertHub.Application.Interfaces;
using AlertHub.Infrastructure.Persistence.Converters;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
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
    public DbSet<OnCallShift> OnCallShifts => Set<OnCallShift>();
    private readonly ITenantContext? _tenantContext = tenantContext;

    /// <summary>
    /// Every timestamp in the domain is a UTC instant, but SQL Server returns datetime2
    /// as DateTimeKind.Unspecified. Such a value serializes without a trailing 'Z', and a
    /// browser client then reads it as local time — so an incident created at 14:00 UTC
    /// displays as 14:00 local, silently wrong by the client's offset. Stamping the kind on
    /// read keeps a UTC instant a UTC instant end to end.
    ///
    /// Pre-convention configuration reaches owned-type properties too, so the TimeWindow
    /// columns on OnCallShift are covered along with everything else.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

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

            // Domain factories assign Guid keys, so EF must not treat them as
            // store-generated: otherwise a new child reached through a tracked
            // parent's navigation is tracked as Modified (its key is already set)
            // and saved as an UPDATE that matches no row.
            var key = entityType.FindPrimaryKey();
            if (key?.Properties is [{ ClrType: var keyClrType } keyProperty] && keyClrType == typeof(Guid))
            {
                keyProperty.ValueGenerated = ValueGenerated.Never;
            }
        }
    }

    private LambdaExpression CreateTenantFilter(Type entityType)
    {
        // Builds: e => _tenantContext != null
        //              && e.TenantId == _tenantContext.TenantId.GetValueOrDefault()
        // When TenantId is null, GetValueOrDefault() returns Guid.Empty,
        // which won't match any entity — effectively filtering everything out.
        var parameter = Expression.Parameter(entityType, "e");
        var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

        var tenantContextField = Expression.Field(
            Expression.Constant(this), "_tenantContext");
        var currentTenantId = Expression.Property(tenantContextField, nameof(ITenantContext.TenantId));

        // Guard against null _tenantContext (e.g. during migrations / design-time)
        var nullCheck = Expression.NotEqual(
            tenantContextField,
            Expression.Constant(null, typeof(ITenantContext)));

        // Safe access — returns Guid.Empty when TenantId is null (no JWT)
        var getValueOrDefault = Expression.Call(currentTenantId, "GetValueOrDefault", Type.EmptyTypes);
        var tenantMatch = Expression.Equal(tenantIdProperty, getValueOrDefault);

        var filter = Expression.AndAlso(nullCheck, tenantMatch);

        return Expression.Lambda(filter, parameter);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantContext?.TenantId;
        if (currentTenantId.HasValue)
        {
            EnforceTenantId(currentTenantId.Value);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        var currentTenantId = _tenantContext?.TenantId;
        if (currentTenantId.HasValue)
        {
            EnforceTenantId(currentTenantId.Value);
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

