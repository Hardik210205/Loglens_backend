using System;

namespace LogLens.Domain.Entities
{
    public class Forecast
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime ForecastTime { get; set; } = DateTime.UtcNow;
        public double PredictedValue { get; set; }
        public string? Notes { get; set; }

        public Guid? IncidentId { get; set; }
        public Incident? Incident { get; set; }
    }
}