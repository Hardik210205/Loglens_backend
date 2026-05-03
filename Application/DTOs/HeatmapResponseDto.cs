namespace LogLens.Application.DTOs
{
    public record HeatmapResponseDto(
        string Hour,
        int Errors,
        int Warnings,
        int Info
    );
}
