using System;
using LogLens.Domain.Enums;

namespace LogLens.Application.DTOs
{
    public record UserDto(Guid Id, string Email, UserRole Role, DateTime CreatedAt, bool IsActive);
}