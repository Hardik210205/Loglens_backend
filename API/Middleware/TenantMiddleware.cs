using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using LogLens.Application.Interfaces;
using System;

namespace LogLens.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            // Extract Tenant ID from Header (e.g. X-Tenant-ID)
            if (context.Request.Headers.TryGetValue("X-Tenant-ID", out var tenantIdStr))
            {
                if (Guid.TryParse(tenantIdStr, out var tenantId))
                {
                    tenantContext.SetTenantId(tenantId);
                }
            }
            // Optional: fallback to JWT Claim
            else if (context.User?.Identity?.IsAuthenticated == true)
            {
                var claim = context.User.FindFirst("TenantId");
                if (claim != null && Guid.TryParse(claim.Value, out var tenantId))
                {
                    tenantContext.SetTenantId(tenantId);
                }
            }

            await _next(context);
        }
    }
}
