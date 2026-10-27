using SkillHub.Domain.Common;

namespace SkillHub.Domain.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchEventsAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
