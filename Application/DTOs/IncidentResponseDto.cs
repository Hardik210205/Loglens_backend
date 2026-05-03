using System;

namespace LogLens.Application.DTOs
{
    public record IncidentResponseDto(
        Guid Id,
        DateTime StartTimeUtc,
        string Severity,
        string Title,
        string Template,
        string ServiceName,
        int ErrorCount,
        int WarningCount,
        DateTime FirstSeen,
        DateTime LastSeen,
        string SuggestedCause,
        string Status
    );
}
