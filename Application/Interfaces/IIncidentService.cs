using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.Application.Interfaces
{
	public interface IIncidentService
	{
		Task<Incident?> CreateRiskIncidentIfNeededAsync(
			string serviceName,
			DateTime startTimeUtc,
			int errorCount,
			int riskScore,
			CancellationToken cancellationToken = default);

		Task<IReadOnlyList<Incident>> GetIncidentsForLast24HoursAsync(CancellationToken cancellationToken = default);

		SeverityLevel ResolveSeverity(int riskScore);
	}
}
