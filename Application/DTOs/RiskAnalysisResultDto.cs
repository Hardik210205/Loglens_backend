namespace LogLens.Application.DTOs
{
    public class RiskAnalysisResultDto
    {
        public int Score { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string AffectedService { get; set; } = string.Empty;
        public string PredictedWindow { get; set; } = string.Empty;
    }
}