using SkillHub.Domain.Errors;

namespace SkillHub.Domain.Exceptions;

public sealed class DuplicateEnrollmentException : BusinessRuleException
{
    public string ParticipantEmail { get; }
    public string WorkshopCode { get; }

    public DuplicateEnrollmentException(string participantEmail, string workshopCode)
        : base(DomainErrors.Enrollment.DuplicateActive(workshopCode))
    {
        ParticipantEmail = participantEmail;
        WorkshopCode = workshopCode;
    }
}
