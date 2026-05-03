using LogLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogLens.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(512);
            builder.Property(u => u.Role).IsRequired().HasConversion<int>();
            builder.Property(u => u.CreatedAt).IsRequired();
            builder.Property(u => u.IsActive).IsRequired();

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("ux_users_email");

            builder.HasMany(u => u.ServicesCreated)
                .WithOne(s => s.CreatedBy)
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}