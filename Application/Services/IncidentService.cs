using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.Application.Services
{
	public class IncidentService : IIncidentService
	{
		private static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(10);
		private const string RiskTemplate = "risk-threshold-breach";

		private readonly IIncidentRepository _incidentRepository;

		public IncidentService(IIncidentRepository incidentRepository)
		{
			_incidentRepository = incidentRepository;
		}

		public async Task<Incident?> CreateRiskIncidentIfNeededAsync(
			string serviceName,
			DateTime startTimeUtc,
			int errorCount,
			int riskScore,
			CancellationToken cancellationToken = default)
		{
			if (riskScore <= 70)
			{
				return null;
			}

			var normalizedServiceName = NormalizeServiceName(serviceName);
			var normalizedStart = startTimeUtc.Kind == DateTimeKind.Utc
				? startTimeUtc
				: startTimeUtc.ToUniversalTime();
			var minStartWindow = normalizedStart.Subtract(DuplicateWindow);

			var existing = await _incidentRepository.FindRecentByServiceAsync(normalizedServiceName, minStartWindow, cancellationToken);
			if (existing != null)
			{
				return existing;
			}

			var severity = ResolveSeverity(riskScore);
			var incident = new Incident
			{
				StartTimeUtc = normalizedStart,
				Severity = severity,
				Title = $"Risk threshold breach in {normalizedServiceName}",
				Description = $"Risk threshold breach in {normalizedServiceName}",
				Template = RiskTemplate,
				ServiceName = normalizedServiceName,
				ErrorCount = Math.Max(0, errorCount),
				WarningCount = 0,
				FirstSeen = normalizedStart,
				LastSeen = DateTime.UtcNow,
				SuggestedCause = "Rapid increase in errors and service impact detected by weighted risk analysis.",
				Status = "Active"
			};

			await _incidentRepository.AddAsync(incident, cancellationToken);
			await _incidentRepository.SaveChangesAsync(cancellationToken);
			return incident;
		}

		public async Task<IReadOnlyList<Incident>> GetIncidentsForLast24HoursAsync(CancellationToken cancellationToken = default)
		{
			var since = DateTime.UtcNow.AddHours(-24);
			var incidents = await _incidentRepository.GetRecentAsync(since, cancellationToken);
			return incidents
				.OrderByDescending(i => i.StartTimeUtc)
				.ToList();
		}

		public SeverityLevel ResolveSeverity(int riskScore)
		{
			if (riskScore >= 90)
			{
				return SeverityLevel.Critical;
			}

			if (riskScore >= 75)
			{
				return SeverityLevel.High;
			}

			if (riskScore >= 50)
			{
				return SeverityLevel.Medium;
			}

			return SeverityLevel.Low;
		}

		private static string NormalizeServiceName(string? serviceName)
		{
			return string.IsNullOrWhiteSpace(serviceName)
				? "UnknownService"
				: serviceName.Trim();
		}
	}
}
