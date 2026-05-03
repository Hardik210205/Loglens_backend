using System;
using LogLens.Domain.Enums;

namespace LogLens.Domain.Entities
{
    public class Alert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public SeverityLevel Severity { get; set; }

        public Guid? IncidentId { get; set; }
        public Incident? Incident { get; set; }

        public Guid? ForecastId { get; set; }
        public Forecast? Forecast { get; set; }
    }
}