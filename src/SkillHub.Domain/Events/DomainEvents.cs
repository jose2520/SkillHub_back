using SkillHub.Domain.Common;

namespace SkillHub.Domain.Events;

public sealed record WorkshopCreatedEvent(
    Guid WorkshopId,
    string Code,
    Guid InstructorId,
    DateTime OccurredOnUtc) : IDomainEvent;

public sealed record EnrollmentCreatedEvent(
    Guid EnrollmentId,
    Guid ParticipantId,
    Guid WorkshopId,
    DateTime OccurredOnUtc) : IDomainEvent;

public sealed record EnrollmentCancelledEvent(
    Guid EnrollmentId,
    Guid ParticipantId,
    Guid WorkshopId,
    string CancelledBy,
    DateTime OccurredOnUtc) : IDomainEvent;
