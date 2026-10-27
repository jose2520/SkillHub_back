using Microsoft.EntityFrameworkCore;
using Npgsql;
using SkillHub.Application.Exceptions;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Common;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UnitOfWork(AppDbContext context, IDomainEventDispatcher domainEventDispatcher)
    {
        _context = context;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }

        int result;
        try
        {
            result = await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("A conflicting active record already exists for this resource.");
        }

        if (domainEvents.Count > 0)
        {
            await _domainEventDispatcher.DispatchEventsAsync(domainEvents, cancellationToken);
        }

        return result;
    }

    public void ClearTrackedChanges()
    {
        _context.ChangeTracker.Clear();
    }
}
