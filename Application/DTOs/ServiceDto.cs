using System;

namespace LogLens.Application.DTOs
{
    public record ServiceDto(Guid Id, string Name, string DisplayName, DateTime CreatedAt, bool IsActive, string OwnerEmail, string KeyPrefix);
}