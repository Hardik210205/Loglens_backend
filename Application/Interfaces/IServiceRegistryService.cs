using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogLens.Application.DTOs;

namespace LogLens.Application.Interfaces
{
    public interface IServiceRegistryService
    {
        Task<CreateServiceResult> CreateServiceAsync(string name, string displayName, Guid ownerUserId);
        Task<ApiKeyResult> GenerateApiKeyAsync(Guid serviceId);
        Task<ApiKeyResult?> RevealCurrentApiKeyAsync(Guid serviceId);
        Task<List<ServiceDto>> GetAllServicesAsync();
        Task<bool> DeleteServiceAsync(Guid serviceId);
        Task<ValidatedService?> ValidateApiKeyAsync(string rawKey);
    }
}