using System;

namespace LogLens.Application.DTOs
{
    public record CreateServiceResult(Guid ServiceId, string Name, string DisplayName, string RawApiKey);
}