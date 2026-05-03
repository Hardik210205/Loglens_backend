using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface IIncidentClusteringService
    {
        /// <summary>
        /// Analyzes logs and creates incidents by clustering similar logs together.
        /// </summary>
        Task<List<Incident>> AnalyzeAndCreateIncidentsAsync(List<LogEntry> logs);

        /// <summary>
        /// Gets the clustering accuracy metric based on test data.
        /// </summary>
        Task<double> GetClusteringAccuracyAsync();
    }
}
