using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Abstractions;

public interface IParticipantRepository : IRepository<Participant>
{
    Task<Participant?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<Participant?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
