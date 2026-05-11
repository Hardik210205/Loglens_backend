using System;
using System.Collections.Generic;

namespace LogLens.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}
