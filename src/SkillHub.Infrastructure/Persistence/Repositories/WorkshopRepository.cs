using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.ValueObjects;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public sealed class WorkshopRepository : GenericRepository<Workshop>, IWorkshopRepository
{
    public WorkshopRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Workshop?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(w => w.Instructor)
            .FirstOrDefaultAsync(w => w.Code == normalizedCode, cancellationToken);

    public async Task<bool> ExistsByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(w => w.Code == normalizedCode, cancellationToken);

    public async Task<Workshop?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Workshop>> GetUpcomingWithDetailsAsync(
        DateTime currentUtc,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = TimeSlot.NormalizeToUtc(currentUtc);

        return await DbSet
            .AsNoTracking()
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .Where(w => w.StartDateTime > nowUtc)
            .OrderBy(w => w.StartDateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Workshop>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(w => w.Instructor)
            .Include(w => w.Enrollments)
            .OrderBy(w => w.StartDateTime)
            .ToListAsync(cancellationToken);
}
