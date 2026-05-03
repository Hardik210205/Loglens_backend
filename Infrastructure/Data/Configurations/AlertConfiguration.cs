using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogLens.Domain.Entities;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class AlertConfiguration : IEntityTypeConfiguration<Alert>
    {
        public void Configure(EntityTypeBuilder<Alert> builder)
        {
            builder.ToTable("alerts");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Timestamp).IsRequired();
            builder.Property(a => a.Message).IsRequired().HasMaxLength(2048);
            builder.Property(a => a.Severity).IsRequired();

            builder.HasOne(a => a.Incident)
                   .WithMany()
                   .HasForeignKey(a => a.IncidentId);

            builder.HasOne(a => a.Forecast)
                   .WithMany()
                   .HasForeignKey(a => a.ForecastId);

            // Add indexes for alert queries
            builder.HasIndex(a => new { a.Timestamp, a.Severity })
                .HasDatabaseName("idx_alerts_timestamp_severity");

            builder.HasIndex(a => a.Severity)
                .HasDatabaseName("idx_alerts_severity");

            builder.HasIndex(a => a.Timestamp)
                .HasDatabaseName("idx_alerts_timestamp")
                .IsDescending();

            builder.HasIndex(a => a.IncidentId)
                .HasDatabaseName("idx_alerts_incidentid");

            builder.HasIndex(a => a.ForecastId)
                .HasDatabaseName("idx_alerts_forecastid");
        }
    }
}