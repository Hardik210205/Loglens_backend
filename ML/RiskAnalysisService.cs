using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;

namespace LogLens.ML
{
    public class RiskAnalysisService
    {
        private readonly MLContext _mlContext;

        public RiskAnalysisService()
        {
            _mlContext = new MLContext(seed: 7);
        }

        /// <summary>
        /// Builds a warning-count time series from log entries and calculates an overall risk score.
        /// </summary>
        public RiskAnalysisResult AnalyzeRiskFromLogs(IEnumerable<LogEntry> logs, int bucketMinutes = 5, int forecastHorizon = 6)
        {
            var series = BuildWarningCountSeries(logs, bucketMinutes);
            return AnalyzeRisk(series, forecastHorizon);
        }

        /// <summary>
        /// Uses SSA forecasting + warning velocity analysis to calculate system risk (0-100).
        /// </summary>
        public RiskAnalysisResult AnalyzeRisk(IReadOnlyList<float> warningSeries, int forecastHorizon = 6)
        {
            var result = new RiskAnalysisResult
            {
                CalculatedAtUtc = DateTime.UtcNow
            };

            if (warningSeries == null || warningSeries.Count < 18)
            {
                result.RiskScorePercent = 0;
                result.RiskCategory = "Low";
                result.Explanation = "Insufficient warning history to calculate risk.";
                return result;
            }

            var cleanedSeries = warningSeries.Select(x => Math.Max(0f, x)).ToList();
            var forecast = ForecastWithSsa(cleanedSeries, forecastHorizon);

            var recentVelocity = ComputeVelocity(cleanedSeries, window: 6);
            var previousVelocity = ComputeVelocity(cleanedSeries.Take(cleanedSeries.Count - 6).ToList(), window: 6);
            var acceleration = recentVelocity - previousVelocity;

            var recentAvg = cleanedSeries.TakeLast(6).Average();
            var baselineAvg = cleanedSeries.Take(Math.Max(1, cleanedSeries.Count - 6)).DefaultIfEmpty(0).Average();
            var growthRatio = baselineAvg <= 0 ? (recentAvg > 0 ? recentAvg : 0) : recentAvg / baselineAvg;

            var forecastAvg = forecast.Count > 0 ? forecast.Average() : recentAvg;
            var forecastRatio = recentAvg <= 0 ? (forecastAvg > 0 ? forecastAvg : 0) : forecastAvg / recentAvg;

            var velocityComponent = Normalize(recentVelocity, 0, 1.2) * 45;
            var accelerationComponent = Normalize(acceleration, 0, 0.8) * 20;
            var trendComponent = Normalize(growthRatio, 1, 3.5) * 20;
            var forecastComponent = Normalize(forecastRatio, 1, 2.5) * 15;

            var riskScore = Math.Clamp(
                velocityComponent + accelerationComponent + trendComponent + forecastComponent,
                0,
                100);

            var exponentialGrowth = growthRatio >= 1.6 && acceleration > 0.08 && recentVelocity > 0.2;
            var criticalForecast = forecastAvg >= Math.Max(recentAvg * 1.5, 20);
            var isCritical = riskScore >= 75 || (exponentialGrowth && criticalForecast);

            result.RiskScorePercent = Math.Round(riskScore, 2);
            result.RiskCategory = isCritical
                ? "Critical"
                : riskScore >= 60
                    ? "High"
                    : riskScore >= 35
                        ? "Medium"
                        : "Low";

            result.IsCriticalRisk = isCritical;
            result.RecentVelocity = Math.Round(recentVelocity, 4);
            result.Acceleration = Math.Round(acceleration, 4);
            result.ForecastedWarningAverage = Math.Round(forecastAvg, 2);
            result.ForecastedWarnings = forecast;
            result.Explanation = isCritical
                ? "Warning velocity is accelerating with strong projected growth (critical risk)."
                : "Risk evaluated using warning velocity, acceleration, and SSA forecast trend.";

            return result;
        }

        private List<float> BuildWarningCountSeries(IEnumerable<LogEntry> logs, int bucketMinutes)
        {
            var items = (logs ?? Enumerable.Empty<LogEntry>())
                .Where(l => l.Level == LogLevel.Warning)
                .OrderBy(l => l.Timestamp)
                .ToList();

            if (items.Count == 0)
            {
                return new List<float>();
            }

            var bucket = TimeSpan.FromMinutes(Math.Max(1, bucketMinutes));
            var start = FloorToBucket(items.First().Timestamp, bucket);
            var end = FloorToBucket(items.Last().Timestamp, bucket);

            var countsByBucket = items
                .GroupBy(l => FloorToBucket(l.Timestamp, bucket))
                .ToDictionary(g => g.Key, g => g.Count());

            var series = new List<float>();
            for (var t = start; t <= end; t = t.Add(bucket))
            {
                countsByBucket.TryGetValue(t, out var count);
                series.Add(count);
            }

            return series;
        }

        private List<double> ForecastWithSsa(IReadOnlyList<float> series, int horizon)
        {
            var data = series.Select(v => new WarningCountPoint { WarningCount = v }).ToList();
            var dataView = _mlContext.Data.LoadFromEnumerable(data);

            var trainSize = data.Count;
            var windowSize = Math.Clamp(Math.Min(12, trainSize / 3), 4, 24);
            var seriesLength = Math.Clamp(Math.Max(windowSize * 2, 24), 24, Math.Max(24, trainSize));

            var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                outputColumnName: nameof(SsaForecastOutput.Forecast),
                inputColumnName: nameof(WarningCountPoint.WarningCount),
                windowSize: windowSize,
                seriesLength: seriesLength,
                trainSize: trainSize,
                horizon: Math.Max(1, horizon),
                confidenceLowerBoundColumn: nameof(SsaForecastOutput.LowerBound),
                confidenceUpperBoundColumn: nameof(SsaForecastOutput.UpperBound),
                confidenceLevel: 0.95f);

            var model = forecastingPipeline.Fit(dataView);
            var engine = model.CreateTimeSeriesEngine<WarningCountPoint, SsaForecastOutput>(_mlContext);
            var prediction = engine.Predict();

            return (prediction.Forecast ?? Array.Empty<float>())
                .Select(v => (double)Math.Max(0, v))
                .ToList();
        }

        private static DateTime FloorToBucket(DateTime value, TimeSpan bucket)
        {
            var ticks = (value.Ticks / bucket.Ticks) * bucket.Ticks;
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static double ComputeVelocity(IReadOnlyList<float> values, int window)
        {
            if (values == null || values.Count < window * 2)
            {
                return 0;
            }

            var previous = values.Skip(values.Count - (window * 2)).Take(window).Average();
            var current = values.TakeLast(window).Average();

            if (previous <= 0)
            {
                return current > 0 ? 1 : 0;
            }

            return (current - previous) / previous;
        }

        private static double Normalize(double value, double min, double max)
        {
            if (value <= min) return 0;
            if (value >= max) return 1;
            return (value - min) / (max - min);
        }

        private class WarningCountPoint
        {
            public float WarningCount { get; set; }
        }

        private class SsaForecastOutput
        {
            [VectorType]
            public float[] Forecast { get; set; } = Array.Empty<float>();

            [VectorType]
            public float[] LowerBound { get; set; } = Array.Empty<float>();

            [VectorType]
            public float[] UpperBound { get; set; } = Array.Empty<float>();
        }
    }

    public class RiskAnalysisResult
    {
        public DateTime CalculatedAtUtc { get; set; }
        public double RiskScorePercent { get; set; }
        public bool IsCriticalRisk { get; set; }
        public string RiskCategory { get; set; } = "Low";
        public double RecentVelocity { get; set; }
        public double Acceleration { get; set; }
        public double ForecastedWarningAverage { get; set; }
        public IReadOnlyList<double> ForecastedWarnings { get; set; } = Array.Empty<double>();
        public string Explanation { get; set; } = string.Empty;
    }
}