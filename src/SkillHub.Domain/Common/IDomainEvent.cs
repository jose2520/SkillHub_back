namespace SkillHub.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
