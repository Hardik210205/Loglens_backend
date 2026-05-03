using System;
using System.Collections.Generic;
using LogLens.Domain.Enums;

namespace LogLens.Domain.Entities
{
    public class Incident
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime StartTimeUtc { get; set; } = DateTime.UtcNow;
        public SeverityLevel Severity { get; set; } = SeverityLevel.Low;
        public string Description { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Template { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public string SuggestedCause { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";

        public ICollection<LogEntry> LogEntries { get; set; } = new List<LogEntry>();
    }
}
