using System;
using LogLens.Application.Interfaces;

namespace LogLens.Infrastructure.Services
{
    public class TenantContext : ITenantContext
    {
        public Guid? CurrentTenantId { get; private set; }

        public void SetTenantId(Guid tenantId)
        {
            CurrentTenantId = tenantId;
        }
    }
}
