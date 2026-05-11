using System;
using System.Collections.Generic;
using LogLens.Domain.Enums;

namespace LogLens.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.Viewer;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public Guid? TenantId { get; set; }
        public Tenant? Tenant { get; set; }

        public ICollection<Service> ServicesCreated { get; set; } = new List<Service>();
    }
}