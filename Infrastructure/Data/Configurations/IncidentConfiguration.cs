using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogLens.Domain.Entities;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
    {
        public void Configure(EntityTypeBuilder<Incident> builder)
        {
            builder.ToTable("incidents");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.StartTimeUtc)
                .HasColumnName("StartTime")
                .IsRequired();
            builder.Property(i => i.Severity)
                .HasConversion<int>()
                .IsRequired();
            builder.Property(i => i.Description).IsRequired().HasMaxLength(2000);
            builder.Property(i => i.Title).IsRequired().HasMaxLength(256);
            builder.Property(i => i.Template).IsRequired().HasMaxLength(2048);
            builder.Property(i => i.ServiceName).IsRequired().HasMaxLength(128);
            builder.Property(i => i.ErrorCount).IsRequired();
            builder.Property(i => i.WarningCount).IsRequired();
            builder.Property(i => i.FirstSeen).IsRequired();
            builder.Property(i => i.LastSeen).IsRequired();
            builder.Property(i => i.SuggestedCause).HasMaxLength(512);
            builder.Property(i => i.Status).IsRequired().HasMaxLength(32);

            builder.HasMany(i => i.LogEntries)
                .WithOne(l => l.Incident)
                .HasForeignKey(l => l.IncidentId);

            builder.HasIndex(i => new { i.StartTimeUtc, i.Severity })
                .HasDatabaseName("idx_incidents_starttime_severity");

            builder.HasIndex(i => i.Severity)
                .HasDatabaseName("idx_incidents_severity");

            builder.HasIndex(i => i.StartTimeUtc)
                .HasDatabaseName("idx_incidents_starttime")
                .IsDescending();

            builder.HasIndex(i => new { i.ServiceName, i.StartTimeUtc, i.Status })
                .HasDatabaseName("idx_incidents_service_start_status");
        }

    }
}