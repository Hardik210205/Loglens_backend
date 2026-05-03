using System;

namespace LogLens.Application.DTOs
{
    /// <summary>
    /// External ingestion contract for /api/logs.
    /// This is what other applications (e.g. PaymentService) send.
    /// </summary>
    public class IngestLogRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string LogLevel { get; set; } = "Information";
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? TraceId { get; set; }
    }
}

