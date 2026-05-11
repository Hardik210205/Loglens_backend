using System;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace LogLens.API.Endpoints
{
    public static class UserEndpoints
    {
        public static void MapUserEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/users")
                .RequireAuthorization("AdminOnly")
                .WithTags("Users");

            group.MapGet("", async (IUserManagementService userManagementService) =>
            {
                var users = await userManagementService.GetAllUsersAsync();
                return Results.Ok(users);
            })
            .WithName("GetUsers");

            group.MapPatch("/{id:guid}/role", async (Guid id, UpdateRoleRequest req, IUserManagementService userManagementService) =>
            {
                var updated = await userManagementService.UpdateUserRoleAsync(id, req.NewRole);
                return updated ? Results.NoContent() : Results.NotFound();
            })
            .WithName("UpdateUserRole");

            group.MapPatch("/{id:guid}/deactivate", async (Guid id, IUserManagementService userManagementService) =>
            {
                var deactivated = await userManagementService.DeactivateUserAsync(id);
                return deactivated ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeactivateUser");

            group.MapDelete("/{id:guid}", async (Guid id, IUserManagementService userManagementService) =>
            {
                var deleted = await userManagementService.DeleteUserAsync(id);
                return deleted ? Results.NoContent() : Results.BadRequest("Cannot delete the last admin or user not found.");
            })
            .WithName("DeleteUser");
        }
    }
}