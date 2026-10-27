using SkillHub.Domain.Common;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Errors;
using SkillHub.Domain.Events;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Domain.Entities;

public class Enrollment : AggregateRoot
{
    public Guid ParticipantId { get; private set; }
    public Participant? Participant { get; private set; }
    public Guid WorkshopId { get; private set; }
    public Workshop? Workshop { get; private set; }
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelledBy { get; private set; }

    protected Enrollment()
    {
    }

    private Enrollment(Guid participantId, Guid workshopId, DateTime enrolledAtUtc)
    {
        ParticipantId = participantId;
        WorkshopId = workshopId;
        Status = EnrollmentStatus.Active;
        EnrolledAt = enrolledAtUtc;
    }

    public static Enrollment Create(Guid participantId, Guid workshopId, DateTime currentUtc)
    {
        if (participantId == Guid.Empty)
            throw new InvalidDomainValueException(nameof(ParticipantId), "Participant ID is required.");

        if (workshopId == Guid.Empty)
            throw new InvalidDomainValueException(nameof(WorkshopId), "Workshop ID is required.");

        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);
        var enrollment = new Enrollment(participantId, workshopId, nowUtc);
        enrollment.AddDomainEvent(new EnrollmentCreatedEvent(enrollment.Id, participantId, workshopId, nowUtc));
        return enrollment;
    }

    public void Cancel(string cancelledBy, bool isEmployee, DateTime workshopStartUtc, DateTime currentUtc)
    {
        if (string.IsNullOrWhiteSpace(cancelledBy))
            throw new InvalidDomainValueException(nameof(CancelledBy), "CancelledBy actor identifier is required.");

        if (Status == EnrollmentStatus.Cancelled)
            throw new BusinessRuleException(DomainErrors.Enrollment.AlreadyCancelled);

        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);
        var startUtc = TimeSlot.NormalizeToUtc(workshopStartUtc);

        if (nowUtc >= startUtc)
            throw new BusinessRuleException(DomainErrors.Enrollment.WorkshopAlreadyStarted);

        if (!isEmployee && (startUtc - nowUtc).TotalHours < 24.0)
            throw new BusinessRuleException(DomainErrors.Enrollment.CancellationWindowClosed);

        Status = EnrollmentStatus.Cancelled;
        CancelledBy = cancelledBy.Trim();
        CancelledAt = nowUtc;

        AddDomainEvent(new EnrollmentCancelledEvent(Id, ParticipantId, WorkshopId, CancelledBy, nowUtc));
    }

    public void EnsureCanGenerateCertificate()
    {
        if (Status != EnrollmentStatus.Active)
            throw new BusinessRuleException(DomainErrors.Enrollment.CertificateRequiresActive);
    }
}
