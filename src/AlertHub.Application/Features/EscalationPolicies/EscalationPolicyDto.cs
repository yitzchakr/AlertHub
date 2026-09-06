using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.EscalationPolicies;

public record EscalationPolicySummaryDto(Guid Id, string Name, int TierCount);

public record EscalationPolicyDetailDto(
    Guid Id,
    string Name,
    List<EscalationTierDto> Tiers);

public record EscalationTierDto(
    Guid Id,
    int Level,
    int EscalateAfterMinutes,
    List<EscalationTierAssignmentDto> Assignments);

/// <summary>
/// Exactly one of the three targeting modes is set. The display name travels with the id
/// because this is read by someone configuring a policy, and an identifier tells them
/// nothing about who they just put on the tier.
/// </summary>
public record EscalationTierAssignmentDto(
    Guid Id,
    Guid? UserId,
    string? UserDisplayName,
    string? UserEmail,
    UserRole? Role,
    bool UseOnCallSchedule);
