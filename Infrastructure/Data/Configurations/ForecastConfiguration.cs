using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogLens.Domain.Entities;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class ForecastConfiguration : IEntityTypeConfiguration<Forecast>
    {
        public void Configure(EntityTypeBuilder<Forecast> builder)
        {
            builder.ToTable("forecasts");
            builder.HasKey(f => f.Id);
            builder.Property(f => f.ForecastTime).IsRequired();
            builder.Property(f => f.PredictedValue).IsRequired();

            builder.HasOne(f => f.Incident)
                   .WithMany()
                   .HasForeignKey(f => f.IncidentId);

            // Add indexes for forecast queries
            builder.HasIndex(f => f.ForecastTime)
                .HasDatabaseName("idx_forecasts_forecasttime")
                .IsDescending();

            builder.HasIndex(f => f.IncidentId)
                .HasDatabaseName("idx_forecasts_incidentid");

            builder.HasIndex(f => f.PredictedValue)
                .HasDatabaseName("idx_forecasts_predictedvalue");
        }
    }
}