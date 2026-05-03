using LogLens.Domain.Enums;

namespace LogLens.Application.DTOs
{
    public record RegisterRequest(string Email, string Password, UserRole Role);
}