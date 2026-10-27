using SkillHub.Domain.Common;
using SkillHub.Domain.Errors;
using SkillHub.Domain.Events;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Domain.Entities;

public class Workshop : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid InstructorId { get; private set; }
    public Instructor? Instructor { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public int Capacity { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private readonly List<Enrollment> _enrollments = new();
    public IReadOnlyCollection<Enrollment> Enrollments => _enrollments.AsReadOnly();

    protected Workshop()
    {
    }

    private Workshop(
        string code,
        string title,
        string description,
        Guid instructorId,
        DateTime startDateTimeUtc,
        DateTime endDateTimeUtc,
        int capacity,
        DateTime createdAtUtc)
    {
        Code = code;
        Title = title;
        Description = description;
        InstructorId = instructorId;
        StartDateTime = startDateTimeUtc;
        EndDateTime = endDateTimeUtc;
        Capacity = capacity;
        CreatedAt = createdAtUtc;
    }

    public static Workshop CreateNew(
        string code,
        string title,
        string description,
        Guid instructorId,
        DateTime startDateTime,
        DateTime endDateTime,
        int capacity,
        DateTime currentUtc)
    {
        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);
        var slot = TimeSlot.Create(startDateTime, endDateTime);

        if (slot.StartDateTime <= nowUtc)
            throw new BusinessRuleException(DomainErrors.Workshop.PastStartDate);

        return BuildValidated(code, title, description, instructorId, slot, capacity, nowUtc, raiseEvent: true);
    }

    public static Workshop CreateSeeded(
        string code,
        string title,
        string description,
        Guid instructorId,
        DateTime startDateTime,
        DateTime endDateTime,
        int capacity,
        DateTime currentUtc)
    {
        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);
        var slot = TimeSlot.Create(startDateTime, endDateTime);

        return BuildValidated(code, title, description, instructorId, slot, capacity, nowUtc, raiseEvent: false);
    }

    private static Workshop BuildValidated(
        string code,
        string title,
        string description,
        Guid instructorId,
        TimeSlot slot,
        int capacity,
        DateTime nowUtc,
        bool raiseEvent)
    {
        var normalizedCode = WorkshopCode.Create(code).Value;

        if (string.IsNullOrWhiteSpace(title))
            throw new InvalidDomainValueException(nameof(Title), "Workshop title is required.");

        if (string.IsNullOrWhiteSpace(description))
            throw new InvalidDomainValueException(nameof(Description), "Workshop description is required.");

        if (instructorId == Guid.Empty)
            throw new InvalidDomainValueException(nameof(InstructorId), "A valid instructor is required.");

        if (capacity <= 0)
            throw new BusinessRuleException(DomainErrors.Workshop.InvalidCapacity);

        var workshop = new Workshop(
            normalizedCode,
            title.Trim(),
            description.Trim(),
            instructorId,
            slot.StartDateTime,
            slot.EndDateTime,
            capacity,
            nowUtc);

        if (raiseEvent)
            workshop.AddDomainEvent(new WorkshopCreatedEvent(workshop.Id, workshop.Code, workshop.InstructorId, nowUtc));

        return workshop;
    }

    public TimeSlot GetTimeSlot() => TimeSlot.Create(StartDateTime, EndDateTime);

    public int CalculateAvailableSpots(int activeEnrollmentsCount) =>
        Math.Max(0, Capacity - activeEnrollmentsCount);

    public void EnsureCanAcceptEnrollment(int activeEnrollmentsCount, DateTime currentUtc)
    {
        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);

        if (StartDateTime <= nowUtc)
            throw new BusinessRuleException(DomainErrors.Workshop.AlreadyStarted(Code));

        if (activeEnrollmentsCount >= Capacity)
            throw new CapacityExceededException(Code, Capacity);
    }
}
