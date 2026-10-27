using Microsoft.EntityFrameworkCore;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;

namespace SkillHub.Infrastructure.Persistence.Repositories;

public sealed class InstructorRepository : GenericRepository<Instructor>, IInstructorRepository
{
    public InstructorRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Instructor?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public async Task<Instructor?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
}

public sealed class ParticipantRepository : GenericRepository<Participant>, IParticipantRepository
{
    public ParticipantRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<Participant?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

    public async Task<Participant?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default) =>
        await DbSet.FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
}
