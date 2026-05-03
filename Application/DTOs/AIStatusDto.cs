using System;

namespace LogLens.Application.DTOs
{
    public record AIStatusDto(
        int ClusteringAccuracy,
        int ForecastingAccuracy,
        int IncidentsDetected,
        int ForecastsGenerated,
        DateTime LastUpdate
    );
}
