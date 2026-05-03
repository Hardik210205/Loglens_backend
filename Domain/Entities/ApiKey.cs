using System;

namespace LogLens.Domain.Entities
{
    public class ApiKey
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid ServiceId { get; set; }
        public string KeyHash { get; set; } = string.Empty;
        public string? RawApiKeyCiphertext { get; set; }
        public string KeyPrefix { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUsedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; } = true;

        public Service? Service { get; set; }
    }
}