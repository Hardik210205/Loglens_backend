using System;

namespace LogLens.Application.DTOs
{
    public record ApiKeyResult(string RawApiKey, string KeyPrefix, Guid ServiceId);
}