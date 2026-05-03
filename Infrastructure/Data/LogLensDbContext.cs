using Microsoft.EntityFrameworkCore;
using LogLens.Domain.Entities;
using LogLens.Infrastructure.Data.Configurations;

namespace LogLens.Infrastructure.Data
{
    public class LogLensDbContext : DbContext
    {
        public LogLensDbContext(DbContextOptions<LogLensDbContext> options) : base(options)
        {
        }

        public DbSet<LogEntry> Logs { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Forecast> Forecasts { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ApiKey> ApiKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LogEntryConfiguration());
            modelBuilder.ApplyConfiguration(new IncidentConfiguration());
            modelBuilder.ApplyConfiguration(new ForecastConfiguration());
            modelBuilder.ApplyConfiguration(new AlertConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new ServiceConfiguration());
            modelBuilder.ApplyConfiguration(new ApiKeyConfiguration());
        }
    }
}
