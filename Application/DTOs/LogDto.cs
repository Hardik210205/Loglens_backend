using System;
using LogLens.Domain.Enums;

namespace LogLens.Application.DTOs
{
    public class LogDto
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public LogLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Metadata { get; set; }
    }
}
