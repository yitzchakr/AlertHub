using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.EscalationPolicies.AddAssignment;

public record AddAssignmentCommand(Guid TierId, Guid? UserId, EscalationRole? EscalationRole, bool UseOnCallSchedule);

public record AddAssignmentResponse(Guid AssignmentId);
