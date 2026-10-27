using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Common;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : Entity
{
    protected readonly AppDbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(AppDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
        await DbSet.AddAsync(entity, cancellationToken);

    public virtual void Update(T entity) =>
        DbSet.Update(entity);

    public virtual void Remove(T entity) =>
        DbSet.Remove(entity);
}
