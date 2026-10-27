using Microsoft.Extensions.Logging;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Common;

namespace SkillHub.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchEventsAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogInformation(
                "Domain event dispatched: {EventType} at {OccurredOnUtc:O}",
                domainEvent.GetType().Name,
                domainEvent.OccurredOnUtc);
        }

        return Task.CompletedTask;
    }
}
