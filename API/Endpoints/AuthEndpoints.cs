using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Enums;
using LogLens.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;

namespace LogLens.API.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/register", async (RegisterRequest req, IAuthService authService, LogLensDbContext dbContext, HttpContext context) =>
            {
                // If the user is logged in, they MUST be an Admin to create another user in their organization.
                // If they are NOT logged in, we allow it so they can create a brand new Organization.
                if (context.User?.Identity?.IsAuthenticated == true && !HasAdminClaim(context.User))
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                var result = await authService.RegisterAsync(req.Email, req.Password, req.Role);
                if (!result.Success)
                {
                    return Results.BadRequest(new { error = result.Error });
                }

                return Results.Ok(result);
            })
            .WithName("RegisterUser")
            .WithTags("Auth");

            app.MapPost("/api/auth/login", async (LoginRequest req, IAuthService authService) =>
            {
                var result = await authService.LoginAsync(req.Email, req.Password);
                if (!result.Success)
                {
                    return Results.Unauthorized();
                }

                return Results.Ok(result);
            })
            .WithName("LoginUser")
            .WithTags("Auth");
        }

        private static bool HasAdminClaim(ClaimsPrincipal user)
        {
            return user?.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
        }
    }
}