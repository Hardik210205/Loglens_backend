using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Domain.Enums;

namespace LogLens.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(string email, string password, UserRole role);
        Task<AuthResult> LoginAsync(string email, string password);
    }
}