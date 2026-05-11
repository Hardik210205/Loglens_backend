using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogLens.Domain.Entities;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
    {
        public void Configure(EntityTypeBuilder<LogEntry> builder)
        {
            builder.ToTable("logs");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Timestamp).IsRequired();
            builder.Property(l => l.Level).IsRequired();
            builder.Property(l => l.Message).IsRequired().HasMaxLength(2048);
            builder.Property(l => l.Metadata).HasColumnType("jsonb");
            builder.Property(l => l.ClusterId).HasMaxLength(64);

            builder.HasOne(l => l.Service)
                .WithMany(s => s.Logs)
                .HasForeignKey(l => l.ServiceId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add composite index for common queries (timestamp + level)
            builder.HasIndex(l => new { l.Timestamp, l.Level })
                .HasDatabaseName("idx_logs_timestamp_level");

            // Add index for timestamp-based queries (for forecasting)
            builder.HasIndex(l => l.Timestamp)
                .HasDatabaseName("idx_logs_timestamp")
                .IsDescending();

            // Add index for level-based filtering
            builder.HasIndex(l => l.Level)
                .HasDatabaseName("idx_logs_level");

            builder.HasIndex(l => l.ClusterId)
                .HasDatabaseName("idx_logs_clusterid");

            builder.HasIndex(l => l.IncidentId)
                .HasDatabaseName("idx_logs_incidentid");

            builder.HasIndex(l => l.ServiceId)
                .HasDatabaseName("idx_logs_serviceid");

            // Table hint for columnstore (comment with guidance)
            // Note: This requires manual ALTER TABLE statement after migration:
            // ALTER TABLE logs ADD CLUSTERED COLUMNSTORE INDEX cx_logs_columnstore WITH (DROP_EXISTING = OFF);
        }
    }
}