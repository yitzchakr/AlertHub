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

    /// <summary>
    /// When this person stopped being staff, or null while they still are. A departure is
    /// recorded rather than deleted: EscalationTierAssignment.UserId is Restrict, so anyone
    /// named on a tier cannot be removed at all, and Incident.AcknowledgedByUserId is
    /// SetNull, so a delete would rewrite answered incidents into unanswered ones.
    ///
    /// Queries must test this column directly — u.DeactivatedAt == null — so the predicate
    /// reaches SQL.
    /// </summary>
    public DateTime? DeactivatedAt { get; private set; }
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

    /// <summary>
    /// Takes somebody off the pager and out of the product. Idempotent, so repeating it
    /// does not move the recorded date.
    ///
    /// The refresh token goes with it. Leaving it in place would let a departed account keep
    /// minting access tokens for the token's remaining week; an access token already issued
    /// still stands until it expires, which is the price of validating them without a
    /// round trip.
    /// </summary>
    public void Deactivate(DateTime deactivatedAtUtc)
    {
        if (DeactivatedAt is not null)
            return;

        DeactivatedAt = deactivatedAtUtc;
        RevokeRefreshToken();
    }

    /// <summary>Puts somebody back. They must sign in again; the old token is gone.</summary>
    public void Reactivate()
    {
        DeactivatedAt = null;
    }
}
