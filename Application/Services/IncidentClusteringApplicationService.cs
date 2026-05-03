using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.Application.Services
{
    public class IncidentClusteringApplicationService : IIncidentClusteringService
    {
        private static readonly TimeSpan IncidentWindow = TimeSpan.FromMinutes(10);
        private const int MinLogsToCreateIncident = 5;

        private readonly ILogSanitizer _logSanitizer;
        private readonly IIncidentRepository _incidentRepository;
        private readonly ILogRepository _logRepository;

        public IncidentClusteringApplicationService(
            IIncidentRepository incidentRepository,
            ILogRepository logRepository,
            ILogSanitizer logSanitizer)
        {
            _incidentRepository = incidentRepository;
            _logRepository = logRepository;
            _logSanitizer = logSanitizer;
        }

        public async Task<List<Incident>> AnalyzeAndCreateIncidentsAsync(List<LogEntry> logs)
        {
            if (logs == null || logs.Count == 0)
                return new List<Incident>();

            var groups = logs
                .GroupBy(l => new
                {
                    ServiceName = string.IsNullOrWhiteSpace(l.ServiceName) ? "UnknownService" : l.ServiceName,
                    Template = _logSanitizer.Sanitize(l.Message)
                })
                .ToList();

            var incidents = new List<Incident>();
            var minLastSeen = DateTime.UtcNow.Subtract(IncidentWindow);

            foreach (var group in groups)
            {
                var groupedLogs = group.OrderBy(l => l.Timestamp).ToList();
                if (groupedLogs.Count == 0)
                {
                    continue;
                }

                var errorCount = CountErrors(groupedLogs);
                if (errorCount == 0)
                {
                    continue;
                }

                var warningCount = CountWarnings(groupedLogs);

                var activeIncident = await _incidentRepository.FindActiveAsync(
                    group.Key.Template,
                    group.Key.ServiceName,
                    minLastSeen);

                // Prevent one-log incident noise: only open a new incident once a group has enough logs.
                if (activeIncident == null && groupedLogs.Count < MinLogsToCreateIncident)
                {
                    continue;
                }

                if (activeIncident == null)
                {
                    var firstSeen = groupedLogs.Min(l => l.Timestamp);
                    var lastSeen = groupedLogs.Max(l => l.Timestamp);

                    activeIncident = new Incident
                    {
                        StartTimeUtc = firstSeen.Kind == DateTimeKind.Utc ? firstSeen : firstSeen.ToUniversalTime(),
                        Severity = ResolveSeverity(errorCount),
                        Title = BuildTitle(group.Key.Template, group.Key.ServiceName, groupedLogs),
                        Description = BuildTitle(group.Key.Template, group.Key.ServiceName, groupedLogs),
                        Template = group.Key.Template,
                        ServiceName = group.Key.ServiceName,
                        ErrorCount = errorCount,
                        WarningCount = warningCount,
                        FirstSeen = firstSeen,
                        LastSeen = lastSeen,
                        SuggestedCause = BuildSuggestedCause(group.Key.Template, groupedLogs),
                        Status = "Active"
                    };

                    await _incidentRepository.AddAsync(activeIncident);
                }
                else
                {
                    activeIncident.ErrorCount += errorCount;
                    activeIncident.WarningCount += warningCount;
                    activeIncident.FirstSeen = MinDate(activeIncident.FirstSeen, groupedLogs.Min(l => l.Timestamp));
                    activeIncident.LastSeen = MaxDate(activeIncident.LastSeen, groupedLogs.Max(l => l.Timestamp));
                    activeIncident.Severity = ResolveSeverity(activeIncident.ErrorCount);
                    activeIncident.Status = "Active";
                }

                var clusterId = activeIncident.Id.ToString("N");
                foreach (var log in groupedLogs)
                {
                    log.ClusterId = clusterId;
                    log.IncidentId = activeIncident.Id;
                    activeIncident.LogEntries.Add(log);
                }

                incidents.Add(activeIncident);
            }

            return incidents
                .GroupBy(i => i.Id)
                .Select(g => g.First())
                .ToList();
        }

        public async Task<double> GetClusteringAccuracyAsync()
        {
            var totalLogs = (await _logRepository.GetLogsSinceAsync(DateTime.UtcNow.AddHours(-24))).Count();
            if (totalLogs == 0)
            {
                return 1;
            }

            var clusteredLogs = (await _logRepository.GetLogsSinceAsync(DateTime.UtcNow.AddHours(-24)))
                .Count(l => !string.IsNullOrWhiteSpace(l.ClusterId));
            return Math.Round((double)clusteredLogs / totalLogs, 2);
        }

        private static string BuildTitle(string template, string serviceName, IEnumerable<LogEntry> logs)
        {
            var levelPrefix = logs.Any(l => l.Level == LogLevel.Critical || l.Level == LogLevel.Error)
                ? "Error Spike"
                : "Warning Pattern";

            var compactTemplate = template.Length > 80 ? template[..80] + "..." : template;
            return $"{levelPrefix} in {serviceName}: {compactTemplate}";
        }

        private static string BuildSuggestedCause(string template, IEnumerable<LogEntry> logs)
        {
            if (template.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                return "Possible downstream latency or dependency timeout.";
            if (template.Contains("database", StringComparison.OrdinalIgnoreCase) || template.Contains("sql", StringComparison.OrdinalIgnoreCase))
                return "Possible database contention or connectivity issue.";
            if (template.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) || template.Contains("forbidden", StringComparison.OrdinalIgnoreCase))
                return "Possible auth token expiration or permission misconfiguration.";

            var topLevel = logs
                .GroupBy(l => l.Level)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault();

            return topLevel switch
            {
                LogLevel.Critical => "Critical failures indicate unstable service behavior. Check recent deployments.",
                LogLevel.Error => "Frequent errors suggest repeated execution-path failure. Inspect dependency health.",
                _ => "Warning pattern detected. Validate service configuration and upstream responses."
            };
        }

        private static int CountErrors(IEnumerable<LogEntry> logs) =>
            logs.Count(l => l.Level == LogLevel.Error || l.Level == LogLevel.Critical);

        private static int CountWarnings(IEnumerable<LogEntry> logs) =>
            logs.Count(l => l.Level == LogLevel.Warning);

        private static DateTime MinDate(DateTime left, DateTime right) => left <= right ? left : right;

        private static DateTime MaxDate(DateTime left, DateTime right) => left >= right ? left : right;

        private static SeverityLevel ResolveSeverity(int errorCount)
        {
            if (errorCount >= 20)
            {
                return SeverityLevel.Critical;
            }

            if (errorCount >= 10)
            {
                return SeverityLevel.High;
            }

            if (errorCount >= 5)
            {
                return SeverityLevel.Medium;
            }

            return SeverityLevel.Low;
        }
    }
}
