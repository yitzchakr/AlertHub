using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.EscalationPolicies;

public record EscalationTargetDto(
    Guid UserId,
    string DisplayName,
    string Email,
    EscalationTargetSource Source);
