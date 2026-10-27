using SkillHub.Domain.Errors;

namespace SkillHub.Domain.Exceptions;

public sealed class ScheduleConflictException : BusinessRuleException
{
    public string ConflictingWorkshopCode { get; }
    public DateTime ConflictingStartUtc { get; }
    public DateTime ConflictingEndUtc { get; }

    public ScheduleConflictException(string conflictingWorkshopCode, DateTime conflictingStartUtc, DateTime conflictingEndUtc)
        : base(DomainErrors.Enrollment.ScheduleConflict(conflictingWorkshopCode, conflictingStartUtc, conflictingEndUtc))
    {
        ConflictingWorkshopCode = conflictingWorkshopCode;
        ConflictingStartUtc = conflictingStartUtc;
        ConflictingEndUtc = conflictingEndUtc;
    }
}
