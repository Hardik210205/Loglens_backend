using System;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LogLens.API.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ApiKeyMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only enforce API key on ingestion endpoint
            // All other /api/logs/* routes (stats, risk, heatmap, etc.) use JWT auth
            var path = context.Request.Path.Value ?? string.Empty;
            var method = context.Request.Method;

            bool isIngestionRoute =
                path.StartsWith("/api/logs/ingest", StringComparison.OrdinalIgnoreCase) ||
                (path.Equals("/api/logs", StringComparison.OrdinalIgnoreCase) && method == "POST");

            if (!isIngestionRoute)
            {
                await _next(context);
                return;
            }

            // From here: validate API key for ingestion
            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var apiKeyValues) ||
                string.IsNullOrWhiteSpace(apiKeyValues.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Missing X-Api-Key header" });
                return;
            }

            var rawKey = apiKeyValues.ToString();

            using var scope = _serviceScopeFactory.CreateScope();
            var serviceRegistryService = scope.ServiceProvider.GetRequiredService<IServiceRegistryService>();
            var validatedService = await serviceRegistryService.ValidateApiKeyAsync(rawKey);

            if (validatedService == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "Invalid or inactive API key" });
                return;
            }

            context.Items["ServiceId"] = validatedService.ServiceId;
            context.Items["ServiceName"] = validatedService.ServiceName;

            await _next(context);
        }
    }
}