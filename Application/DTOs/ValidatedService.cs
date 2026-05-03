using System;

namespace LogLens.Application.DTOs
{
    public record ValidatedService(Guid ServiceId, string ServiceName);
}