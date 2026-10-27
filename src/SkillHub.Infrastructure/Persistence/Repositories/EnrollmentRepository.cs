using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public sealed class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
{
    public EnrollmentRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Enrollment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
                .ThenInclude(w => w!.Instructor)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetByParticipantIdWithDetailsAsync(
        Guid participantId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
                .ThenInclude(w => w!.Instructor)
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetByWorkshopIdWithDetailsAsync(
        Guid workshopId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(e => e.Participant)
            .Include(e => e.Workshop)
                .ThenInclude(w => w!.Instructor)
            .Where(e => e.WorkshopId == workshopId)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Enrollment>> GetActiveByParticipantIdWithWorkshopAsync(
        Guid participantId,
        CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(e => e.Workshop)
            .Where(e => e.ParticipantId == participantId && e.Status == EnrollmentStatus.Active)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasActiveEnrollmentAsync(
        Guid participantId,
        Guid workshopId,
        CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            e => e.ParticipantId == participantId
                 && e.WorkshopId == workshopId
                 && e.Status == EnrollmentStatus.Active,
            cancellationToken);

    public async Task<int> CountActiveByWorkshopIdAsync(
        Guid workshopId,
        CancellationToken cancellationToken = default) =>
        await DbSet.CountAsync(
            e => e.WorkshopId == workshopId && e.Status == EnrollmentStatus.Active,
            cancellationToken);
}
