using Microsoft.EntityFrameworkCore;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Data.Configurations;
using LogLens.Application.Interfaces;
using System;

namespace LogLens.Infrastructure.Data
{
    public class LogLensDbContext : DbContext
    {
        private readonly ITenantContext _tenantContext;

        public LogLensDbContext(DbContextOptions<LogLensDbContext> options, ITenantContext tenantContext) : base(options)
        {
            _tenantContext = tenantContext;
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        public Guid? CurrentTenantId => _tenantContext?.CurrentTenantId;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LogEntryConfiguration());
            modelBuilder.ApplyConfiguration(new IncidentConfiguration());
            modelBuilder.ApplyConfiguration(new ForecastConfiguration());
            modelBuilder.ApplyConfiguration(new AlertConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceConfiguration());
            modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());

            // Multi-Tenancy Global Query Filters
            // We MUST use the DbContext property (CurrentTenantId) so EF Core evaluates it per-query, 
            // rather than capturing a static null value at app startup!
            modelBuilder.Entity<LogEntry>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<Service>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<User>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
            modelBuilder.Entity<ApiKey>().HasQueryFilter(e => !CurrentTenantId.HasValue || e.TenantId == CurrentTenantId);
        }
    }
}
