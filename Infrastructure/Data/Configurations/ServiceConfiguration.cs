using LogLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("services");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Name).IsRequired().HasMaxLength(128);
            builder.Property(s => s.DisplayName).IsRequired().HasMaxLength(256);
            builder.Property(s => s.CreatedAt).IsRequired();
            builder.Property(s => s.IsActive).IsRequired();

            builder.HasIndex(s => s.Name)
                .IsUnique()
                .HasDatabaseName("ux_services_name");

            builder.HasMany(s => s.ApiKeys)
                .WithOne(a => a.Service)
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}