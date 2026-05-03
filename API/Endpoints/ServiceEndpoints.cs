using System;
using System.Security.Claims;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using Microsoft.AspNetCore.Builder;

namespace LogLens.API.Endpoints
{
    public static class ServiceEndpoints
    {
        public static void MapServiceEndpoints(this WebApplication app)
        {
            var group = app.MapGroup("/api/services")
                .RequireAuthorization("AdminOnly")
                .WithTags("Services");

            group.MapPost("", async (CreateServiceRequest req, IServiceRegistryService serviceRegistryService, ClaimsPrincipal user) =>
            {
                if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"), out var ownerUserId))
                {
                    return Results.Unauthorized();
                }

                var result = await serviceRegistryService.CreateServiceAsync(req.Name, req.DisplayName, ownerUserId);
                // RawApiKey is shown once in this response and must never be stored in plain text.
                return Results.Created($"/api/services/{result.ServiceId}", result);
            })
            .WithName("CreateService");

            group.MapGet("", async (IServiceRegistryService serviceRegistryService) =>
            {
                var services = await serviceRegistryService.GetAllServicesAsync();
                return Results.Ok(services);
            })
            .WithName("GetServices");

            group.MapDelete("/{id:guid}", async (Guid id, IServiceRegistryService serviceRegistryService) =>
            {
                var deleted = await serviceRegistryService.DeleteServiceAsync(id);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteService");

            group.MapPost("/{id:guid}/rotate", async (Guid id, IServiceRegistryService serviceRegistryService) =>
            {
                var result = await serviceRegistryService.GenerateApiKeyAsync(id);
                // RawApiKey is shown once in this response and must never be stored in plain text.
                return Results.Ok(result);
            })
            .WithName("RotateServiceApiKey");

            group.MapGet("/{id:guid}/key", async (Guid id, IServiceRegistryService serviceRegistryService) =>
            {
                var result = await serviceRegistryService.RevealCurrentApiKeyAsync(id);
                return result is null
                    ? Results.NotFound(new { message = "Current API key is not available. Rotate the key to generate a new recoverable key." })
                    : Results.Ok(result);
            })
            .WithName("RevealServiceApiKey");
        }
    }
}