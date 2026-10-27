using SkillHub.Domain.Common;
using SkillHub.Domain.Errors;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Domain.ValueObjects;

public sealed class TimeSlot : ValueObject
{
    public DateTime StartDateTime { get; }
    public DateTime EndDateTime { get; }

    private TimeSlot(DateTime startDateTime, DateTime endDateTime)
    {
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
    }

    public static TimeSlot Create(DateTime startDateTime, DateTime endDateTime)
    {
        var startUtc = NormalizeToUtc(startDateTime);
        var endUtc = NormalizeToUtc(endDateTime);

        if (endUtc <= startUtc)
            throw new BusinessRuleException(DomainErrors.Workshop.InvalidSchedule);

        return new TimeSlot(startUtc, endUtc);
    }

    public bool OverlapsWith(TimeSlot other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return StartDateTime < other.EndDateTime && other.StartDateTime < EndDateTime;
    }

    public static DateTime NormalizeToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return StartDateTime;
        yield return EndDateTime;
    }
}
