using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LogLens.Application.DTOs;

namespace LogLens.Application.Interfaces
{
    public interface IRiskAnalysisService
    {
        Task<RiskAnalysisResultDto> AnalyzeSystemRiskAsync(CancellationToken cancellationToken = default);
        Task<List<ServiceRiskInsightDto>> GetTopFailingServicesAsync(int top = 5, CancellationToken cancellationToken = default);
        Task<List<ErrorTrendPointDto>> GetErrorTrendPredictionAsync(int bucketMinutes = 5, int historyBuckets = 12, int projectionBuckets = 3, CancellationToken cancellationToken = default);
    }
}