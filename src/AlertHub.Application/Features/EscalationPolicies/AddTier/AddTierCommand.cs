namespace AlertHub.Application.Features.EscalationPolicies.AddTier;

public record AddTierCommand(Guid PolicyId, int Level, int EscalateAfterMinutes);

public record AddTierResponse(Guid TierId, int Level, int EscalateAfterMinutes);
