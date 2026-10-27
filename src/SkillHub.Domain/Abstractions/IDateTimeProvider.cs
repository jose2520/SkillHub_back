namespace SkillHub.Domain.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
