using System;
using System.Collections.Generic;
using System.Text;
using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AlertHub.Infrastructure.Persistence;

    public class AlertHubDbContext:DbContext
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
    public AlertHubDbContext(DbContextOptions<AlertHubDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AlertHubDbContext).Assembly);
    }
}

