namespace AlertHub.Application.Features.EscalationPolicies.CreateEscalationPolicy;

public record CreateEscalationPolicyCommand(string Name);

public record CreateEscalationPolicyResponse(Guid Id, string Name);
