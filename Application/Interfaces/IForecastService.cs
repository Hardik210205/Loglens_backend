using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogLens.Domain.Entities;

namespace LogLens.Application.Interfaces
{
    public interface IForecastService
    {
        /// <summary>
        /// Forecasts future warning/error log frequency based on historical data.
        /// </summary>
        Task<List<ForecastData>> ForecastWarningsAsync(int hoursAhead = 24);

        /// <summary>
        /// Creates alerts based on forecast predictions.
        /// </summary>
        Task<List<Alert>> GenerateAlertsAsync(List<ForecastData> forecasts);

        /// <summary>
        /// Gets the forecast model accuracy metric.
        /// </summary>
        Task<double> GetForecastAccuracyAsync();
    }

    public class ForecastData
    {
        public DateTime Timestamp { get; set; }
        public double PredictedValue { get; set; }
        public double ConfidenceLower { get; set; }
        public double ConfidenceUpper { get; set; }
    }
}
