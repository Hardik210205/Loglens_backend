using System;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.Application.Services
{
    public class LogService : ILogService
    {
        private readonly ILogQueueService _queueService;

        public LogService(ILogQueueService queueService)
        {
            _queueService = queueService;
        }

        public async Task EnqueueAsync(IngestLogRequest log)
        {
            var detectedLevel = DetectLevel(log.LogLevel, log.Message);
            var normalizedMessage = NormalizeMessage(log.ServiceName, log.Message, detectedLevel);

            var entry = new LogEntry
            {
                Timestamp = AnalyticsTime.NormalizeUtc(log.Timestamp),
                Level = detectedLevel,
                Message = normalizedMessage,
                ServiceName = log.ServiceName,
                TraceId = log.TraceId
            };

            await _queueService.EnqueueAsync(entry);
        }

        private static LogLevel ParseLogLevel(string level) =>
            Enum.TryParse<LogLevel>(level, ignoreCase: true, out var parsed)
                ? parsed
                : LogLevel.Information;

        private static LogLevel DetectLevel(string level, string? message)
        {
            var parsed = ParseLogLevel(level);
            var msg = (message ?? string.Empty).Trim();
            var lower = msg.ToLowerInvariant();

            if (lower.Contains("failed") || lower.Contains("error") || lower.Contains("invalid amount"))
            {
                return LogLevel.Error;
            }

            if (lower.Contains("retry"))
            {
                return LogLevel.Warning;
            }

            if (lower.Contains("success") || lower.Contains("processed successfully"))
            {
                return LogLevel.Information;
            }

            return parsed;
        }

        private static string NormalizeMessage(string? serviceName, string? message, LogLevel level)
        {
            var input = (message ?? string.Empty).Trim();
            var isPaymentContext =
                (!string.IsNullOrWhiteSpace(serviceName) && serviceName.Contains("payment", StringComparison.OrdinalIgnoreCase)) ||
                input.Contains("payment", StringComparison.OrdinalIgnoreCase);

            if (!isPaymentContext)
            {
                return string.IsNullOrWhiteSpace(input) ? "Log received" : input;
            }

            return level switch
            {
                LogLevel.Error or LogLevel.Critical => "Payment failed due to invalid amount",
                LogLevel.Warning => "Payment retry initiated",
                _ => "Payment processed successfully"
            };
        }
    }
}
