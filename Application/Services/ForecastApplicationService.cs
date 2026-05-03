using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;
using LogLens.ML.Forecasting;

namespace LogLens.Application.Services
{
    public class ForecastApplicationService : IForecastService
    {
        private readonly WarningForecastService _mlService;
        private readonly ILogRepository _logRepository;
        private readonly IForecastRepository _forecastRepository;

        public ForecastApplicationService(ILogRepository logRepository, IForecastRepository forecastRepository)
        {
            _mlService = new WarningForecastService();
            _logRepository = logRepository;
            _forecastRepository = forecastRepository;
        }

        public async Task<List<ForecastData>> ForecastWarningsAsync(int hoursAhead = 24)
        {
            try
            {
                // Get logs from the past 7 days for training
                var historicalLogs = (await _logRepository.GetLogsSinceAsync(DateTime.UtcNow.AddDays(-7))).ToList();

                if (historicalLogs.Count < 10)
                    return new List<ForecastData>();

                // Run ML forecast
                var forecasts = _mlService.ForecastWarnings(historicalLogs.ToList(), hoursAhead);

                // Save forecasts to database
                var forecastEntities = forecasts.Select(f => new Forecast
                {
                    ForecastTime = f.Timestamp,
                    PredictedValue = f.Value,
                    Notes = $"Confidence: [{f.ConfidenceLower:F2}, {f.ConfidenceUpper:F2}]"
                }).ToList();

                foreach (var forecast in forecastEntities)
                {
                    await _forecastRepository.AddAsync(forecast);
                }

                return forecasts.Select(f => new ForecastData
                {
                    Timestamp = f.Timestamp,
                    PredictedValue = f.Value,
                    ConfidenceLower = f.ConfidenceLower,
                    ConfidenceUpper = f.ConfidenceUpper
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ForecastWarningsAsync: {ex.Message}");
                return new List<ForecastData>();
            }
        }

        public Task<List<Alert>> GenerateAlertsAsync(List<ForecastData> forecasts)
        {
            if (forecasts.Count == 0)
                return Task.FromResult(new List<Alert>());

            var mlAlerts = _mlService.GenerateAlertsFromForecast(
                forecasts.Select(f => new LogLens.ML.Forecasting.ForecastResult
                {
                    Timestamp = f.Timestamp,
                    Value = f.PredictedValue,
                    ConfidenceLower = f.ConfidenceLower,
                    ConfidenceUpper = f.ConfidenceUpper
                }).ToList()
            );

            return Task.FromResult(mlAlerts);
        }

        public async Task<double> GetForecastAccuracyAsync()
        {
            var logs = (await _logRepository.GetLogsSinceAsync(DateTime.UtcNow.AddDays(-7))).ToList();
            var accuracy = _mlService.ComputeForecastAccuracy(logs);
            return Math.Round(accuracy, 2);
        }
    }
}
