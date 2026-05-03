using LogLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
    {
        public void Configure(EntityTypeBuilder<ApiKey> builder)
        {
            builder.ToTable("api_keys");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.ServiceId).IsRequired();
            builder.Property(a => a.KeyHash).IsRequired().HasMaxLength(512);
            builder.Property(a => a.RawApiKeyCiphertext).HasMaxLength(2048);
            builder.Property(a => a.KeyPrefix).IsRequired().HasMaxLength(16);
            builder.Property(a => a.Description).HasMaxLength(256);
            builder.Property(a => a.CreatedAt).IsRequired();
            builder.Property(a => a.IsActive).IsRequired();

            builder.HasIndex(a => a.KeyPrefix)
                .HasDatabaseName("idx_api_keys_prefix");

            builder.HasIndex(a => new { a.ServiceId, a.IsActive })
                .HasDatabaseName("idx_api_keys_service_active");
        }
    }
}