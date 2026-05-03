using LogLens.Domain.Enums;

namespace LogLens.Application.DTOs
{
    public record AuthResult(bool Success, string? Token, string? Email, UserRole? Role, string? Error);
}