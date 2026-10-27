using SkillHub.Domain.Errors;

namespace SkillHub.Domain.Exceptions;

public sealed class CapacityExceededException : BusinessRuleException
{
    public string WorkshopCode { get; }
    public int Capacity { get; }

    public CapacityExceededException(string workshopCode, int capacity)
        : base(DomainErrors.Workshop.CapacityExceeded(workshopCode, capacity))
    {
        WorkshopCode = workshopCode;
        Capacity = capacity;
    }
}
