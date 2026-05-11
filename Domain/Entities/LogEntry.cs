using System;
using LogLens.Domain.Enums;

namespace LogLens.Domain.Entities
{
    public class LogEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Metadata { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string? TraceId { get; set; }
        public string? ClusterId { get; set; }
        public Guid? IncidentId { get; set; }
        public Incident? Incident { get; set; }
        public Guid? ServiceId { get; set; }
        public Service? Service { get; set; }
        
        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }
    }
}
