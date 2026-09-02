namespace AlertHub.Application.Features.EscalationPolicies;

/// <summary>How an assignment selected this person.</summary>
public enum EscalationTargetSource
{
    DirectUser,
    Role,
    OnCall
}

public record EscalationTargetDto(
    Guid UserId,
    string DisplayName,
    string Email,
    EscalationTargetSource Source);
