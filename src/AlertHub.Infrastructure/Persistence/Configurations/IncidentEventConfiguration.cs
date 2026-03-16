using AlertHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Infrastructure.Persistence.Configurations;

public class IncidentEventConfiguration : IEntityTypeConfiguration<IncidentEvent>
{
    public void Configure(EntityTypeBuilder<IncidentEvent> builder)
    {
       builder.HasKey(e => e.Id);
       builder.Property(e => e.EventType).HasConversion<string>().HasMaxLength(70);
       builder.Property(e => e.Description).IsRequired();

        builder.HasIndex(e => new { e.IncidentId, e.OccurredAt });
        

    

    }
}

