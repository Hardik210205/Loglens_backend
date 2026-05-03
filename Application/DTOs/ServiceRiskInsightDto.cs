namespace LogLens.Application.DTOs
{
    public class ServiceRiskInsightDto
    {
        public string ServiceName { get; set; } = string.Empty;
        public int ErrorRate { get; set; }
        public string Trend { get; set; } = "0%";
        public int IncidentCount { get; set; }
    }
}