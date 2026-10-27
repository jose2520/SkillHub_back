using SkillHub.Domain.Entities;

namespace SkillHub.Domain.Abstractions;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByParticipantIdWithDetailsAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetByWorkshopIdWithDetailsAsync(Guid workshopId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Enrollment>> GetActiveByParticipantIdWithWorkshopAsync(Guid participantId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveEnrollmentAsync(Guid participantId, Guid workshopId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByWorkshopIdAsync(Guid workshopId, CancellationToken cancellationToken = default);
}
