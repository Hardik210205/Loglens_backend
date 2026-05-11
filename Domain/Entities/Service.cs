using System;
using System.Collections.Generic;

namespace LogLens.Domain.Entities
{
    public class Service
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public Guid CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public User? CreatedBy { get; set; }
        public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
        public ICollection<LogEntry> Logs { get; set; } = new List<LogEntry>();
    }
}