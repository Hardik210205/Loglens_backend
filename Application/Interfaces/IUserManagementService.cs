using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Domain.Enums;

namespace LogLens.Application.Interfaces
{
    public interface IUserManagementService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<bool> UpdateUserRoleAsync(Guid userId, UserRole newRole);
        Task<bool> DeactivateUserAsync(Guid userId);
        Task<bool> DeleteUserAsync(Guid userId);
    }
}