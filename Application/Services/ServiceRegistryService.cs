using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LogLens.Application.DTOs;
using LogLens.Application.Interfaces;
using LogLens.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogLens.Application.Services
{
    public class ServiceRegistryService : IServiceRegistryService
    {
        private static readonly Regex SlugRegex = new("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IConfiguration _configuration;
        private readonly DbContext _dbContext;
        private readonly IApiKeyCipher _apiKeyCipher;

        public ServiceRegistryService(IConfiguration configuration, DbContext dbContext, IApiKeyCipher apiKeyCipher)
        {
            _configuration = configuration;
            _dbContext = dbContext;
            _apiKeyCipher = apiKeyCipher;
        }

        public async Task<CreateServiceResult> CreateServiceAsync(string name, string displayName, Guid ownerUserId)
        {
            var normalizedName = NormalizeSlug(name);
            var normalizedDisplayName = (displayName ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(normalizedName) || string.IsNullOrWhiteSpace(normalizedDisplayName))
            {
                throw new ArgumentException("Service name and display name are required.");
            }

            if (!SlugRegex.IsMatch(normalizedName))
            {
                throw new ArgumentException("Service name must be a lowercase slug using letters, numbers, and hyphens only.");
            }

            var services = _dbContext.Set<Service>();
            var existing = await services.AnyAsync(s => s.Name == normalizedName);
            if (existing)
            {
                throw new InvalidOperationException("A service with this name already exists.");
            }

            var userExists = await _dbContext.Set<User>().AnyAsync(u => u.Id == ownerUserId && u.IsActive);
            if (!userExists)
            {
                throw new InvalidOperationException("Owner user was not found or is inactive.");
            }

            var service = new Service
            {
                Name = normalizedName,
                DisplayName = normalizedDisplayName,
                CreatedByUserId = ownerUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await services.AddAsync(service);
            await _dbContext.SaveChangesAsync();

            var apiKeyResult = await GenerateApiKeyAsync(service.Id);
            return new CreateServiceResult(service.Id, service.Name, service.DisplayName, apiKeyResult.RawApiKey);
        }

        public async Task<ApiKeyResult> GenerateApiKeyAsync(Guid serviceId)
        {
            var service = await _dbContext.Set<Service>().FirstOrDefaultAsync(s => s.Id == serviceId);
            if (service == null)
            {
                throw new InvalidOperationException("Service not found.");
            }

            var apiKeys = _dbContext.Set<ApiKey>();
            var existingKeys = await apiKeys.Where(a => a.ServiceId == serviceId && a.IsActive).ToListAsync();
            foreach (var existing in existingKeys)
            {
                existing.IsActive = false;
            }

            var rawApiKey = GenerateRawApiKey();
            var keyPrefix = rawApiKey.Substring(0, 8);

            var apiKey = new ApiKey
            {
                ServiceId = serviceId,
                KeyPrefix = keyPrefix,
                KeyHash = BCrypt.Net.BCrypt.HashPassword(rawApiKey),
                RawApiKeyCiphertext = _apiKeyCipher.Protect(rawApiKey),
                Description = string.Empty,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await apiKeys.AddAsync(apiKey);
            await _dbContext.SaveChangesAsync();

            return new ApiKeyResult(rawApiKey, keyPrefix, serviceId);
        }

        public async Task<ApiKeyResult?> RevealCurrentApiKeyAsync(Guid serviceId)
        {
            var apiKey = await _dbContext.Set<ApiKey>()
                .Where(a => a.ServiceId == serviceId && a.IsActive)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            if (apiKey == null)
            {
                return null;
            }

            var rawApiKey = _apiKeyCipher.Unprotect(apiKey.RawApiKeyCiphertext);
            if (string.IsNullOrWhiteSpace(rawApiKey))
            {
                return null;
            }

            return new ApiKeyResult(rawApiKey, apiKey.KeyPrefix, serviceId);
        }

        public async Task<List<ServiceDto>> GetAllServicesAsync()
        {
            var services = await _dbContext.Set<Service>()
                .Include(s => s.CreatedBy)
                .Include(s => s.ApiKeys)
                .ToListAsync();

            return services
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ServiceDto(
                    s.Id,
                    s.Name,
                    s.DisplayName,
                    s.CreatedAt,
                    s.IsActive,
                    s.CreatedBy?.Email ?? string.Empty,
                    s.ApiKeys.Where(a => a.IsActive)
                        .OrderByDescending(a => a.CreatedAt)
                        .Select(a => a.KeyPrefix)
                        .FirstOrDefault() ?? string.Empty))
                .ToList();
        }

        public async Task<bool> DeleteServiceAsync(Guid serviceId)
        {
            var service = await _dbContext.Set<Service>()
                .Include(s => s.ApiKeys)
                .FirstOrDefaultAsync(s => s.Id == serviceId);

            if (service == null)
            {
                return false;
            }

            service.IsActive = false;
            foreach (var apiKey in service.ApiKeys)
            {
                apiKey.IsActive = false;
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<ValidatedService?> ValidateApiKeyAsync(string rawKey)
        {
            if (string.IsNullOrWhiteSpace(rawKey) || rawKey.Length < 8)
            {
                return null;
            }

            var prefix = rawKey.Substring(0, 8);
            var candidates = await _dbContext.Set<ApiKey>()
                .Include(a => a.Service)
                .Where(a => a.KeyPrefix == prefix && a.IsActive && a.Service != null && a.Service.IsActive)
                .ToListAsync();

            foreach (var candidate in candidates)
            {
                if (BCrypt.Net.BCrypt.Verify(rawKey, candidate.KeyHash))
                {
                    candidate.LastUsedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    return new ValidatedService(candidate.ServiceId, candidate.Service?.Name ?? string.Empty);
                }
            }

            return null;
        }

        private static string GenerateRawApiKey() => "ll_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        private static string NormalizeSlug(string? value) => (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}