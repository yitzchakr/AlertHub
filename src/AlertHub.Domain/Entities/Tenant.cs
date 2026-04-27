using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Domain.Entities;

public class Tenant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    // EF Core navigation
    public ICollection<User> Users { get; private set; } = [];
    public ICollection<Service> Services { get; private set; } = [];

    // Private parameterless constructor for EF Core
    private Tenant() { }

    public static Tenant Create(string name)
    {
        return new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateName(string name)
    {
        Name = name;
    }
}
