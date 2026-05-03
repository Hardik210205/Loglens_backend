using System;

namespace LogLens.Application.DTOs
{
    public class ErrorTrendPointDto
    {
        public DateTime BucketStartUtc { get; set; }
        public string Label { get; set; } = string.Empty;
        public int CurrentErrors { get; set; }
        public int PredictedErrors { get; set; }
    }
}