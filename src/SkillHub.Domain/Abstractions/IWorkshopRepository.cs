using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Abstractions;

public interface IWorkshopRepository : IRepository<Workshop>
{
    Task<Workshop?> GetByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string normalizedCode, CancellationToken cancellationToken = default);
    Task<Workshop?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetUpcomingWithDetailsAsync(DateTime currentUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Workshop>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}
