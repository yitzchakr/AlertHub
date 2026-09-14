namespace AlertHub.Application.Features.Users.SetUserActivation;

/// <summary>
/// One command for both directions so the two paths cannot drift — the interesting rules
/// (who may be deactivated, what happens to their token) belong to the pair, not to one.
///
/// RequestedByUserId is filled from the caller's token at the endpoint, never from a body.
/// </summary>
public record SetUserActivationCommand(Guid UserId, bool Active, Guid RequestedByUserId);

public record SetUserActivationResponse(Guid Id, string DisplayName, DateTime? DeactivatedAt);
