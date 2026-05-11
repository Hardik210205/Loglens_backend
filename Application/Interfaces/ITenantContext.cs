using System;

namespace LogLens.Application.Interfaces
{
    public interface ITenantContext
    {
        Guid? CurrentTenantId { get; }
        void SetTenantId(Guid tenantId);
    }
}
