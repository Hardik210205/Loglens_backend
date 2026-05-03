using System;

namespace LogLens.Application.DTOs
{
    public record LogResponseDto(
        Guid Id,
        DateTime Timestamp,
        string Level,
        string Message,
        string? Metadata
    );
}
