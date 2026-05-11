using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using LogLens.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogLens.Application.Services
{
    public class UserManagementService : IUserManagementService
    {
        private readonly IConfiguration _configuration;
        private readonly DbContext _dbContext;

        public UserManagementService(IConfiguration configuration, DbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _dbContext.Set<User>()
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return users
                .Select(u => new UserDto(u.Id, u.Email, u.Role, u.CreatedAt, u.IsActive))
                .ToList();
        }

        public async Task<bool> UpdateUserRoleAsync(Guid userId, UserRole newRole)
        {
            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            user.Role = newRole;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(Guid userId)
        {
            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            if (user.Role == UserRole.Admin)
            {
                var activeAdminCount = await _dbContext.Set<User>()
                    .CountAsync(u => u.IsActive && u.Role == UserRole.Admin);

                if (activeAdminCount <= 1)
                {
                    return false;
                }
            }

            user.IsActive = false;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            var user = await _dbContext.Set<User>().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return false;
            }

            if (user.Role == UserRole.Admin)
            {
                var activeAdminCount = await _dbContext.Set<User>()
                    .CountAsync(u => u.Role == UserRole.Admin);

                if (activeAdminCount <= 1)
                {
                    return false; // Cannot delete the last admin
                }
            }

            _dbContext.Set<User>().Remove(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}