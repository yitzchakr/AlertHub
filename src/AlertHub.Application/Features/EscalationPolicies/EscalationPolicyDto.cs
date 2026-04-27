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

public record EscalationTierAssignmentDto(
    Guid Id,
    Guid? UserId,
    UserRole? Role,
    bool UseOnCallSchedule);
