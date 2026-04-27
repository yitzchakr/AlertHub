using AlertHub.Domain.Entities;

namespace AlertHub.Application.Features.EscalationPolicies.AddAssignment;

public record AddAssignmentCommand(Guid TierId, Guid? UserId, UserRole? Role, bool UseOnCallSchedule);

public record AddAssignmentResponse(Guid AssignmentId);
