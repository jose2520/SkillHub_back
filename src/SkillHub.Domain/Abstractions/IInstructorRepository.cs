using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Abstractions;

public interface IInstructorRepository : IRepository<Instructor>
{
    Task<Instructor?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<Instructor?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
