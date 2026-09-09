namespace AlertHub.Domain.Entities;

public class User : ITenantEntity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;

    /// <summary>What this user may do. Drives the "role" claim and every RequireRole check.</summary>
    public PermissionRole PermissionRole { get; private set; }

    /// <summary>Which role sweep pages this user. Independent of what they may do.</summary>
    public EscalationRole EscalationRole { get; private set; }

    public string PasswordHash { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }


    // Navigation
    public Tenant Tenant { get; private set; } = default!;

    private User() { }

    public static User Create(
        Guid tenantId,
        string email,
        string displayName,
        string passwordHash,
        PermissionRole permissionRole,
        EscalationRole escalationRole)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            PermissionRole = permissionRole,
            EscalationRole = escalationRole,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetRefreshToken(string token, DateTime expiry)
    {
        RefreshToken = token;
        RefreshTokenExpiryTime = expiry;
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
    }
}
