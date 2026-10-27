using SkillHub.Application.DTOs;

namespace SkillHub.Application.Interfaces;

public interface IWorkshopService
{
    Task<IReadOnlyList<WorkshopResponseDto>> GetUpcomingWorkshopsAsync(CancellationToken cancellationToken = default);
    Task<WorkshopResponseDto> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkshopResponseDto> CreateWorkshopAsync(CreateWorkshopRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnrollmentResponseDto>> GetWorkshopEnrollmentsAsync(Guid workshopId, CancellationToken cancellationToken = default);
}

public interface IEnrollmentService
{
    Task<EnrollmentResponseDto> EnrollAuthenticatedParticipantAsync(
        string identityUserId,
        CreateEnrollmentRequestDto request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentResponseDto>> GetMyEnrollmentsAsync(
        string identityUserId,
        CancellationToken cancellationToken = default);

    Task<EnrollmentResponseDto> CancelEnrollmentAsync(
        Guid enrollmentId,
        string callerIdentityUserId,
        string callerEmail,
        bool isEmployee,
        CancellationToken cancellationToken = default);

    Task<BulkImportResultDto> ImportEnrollmentsAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default);

    Task<CertificateFileDto> GenerateCertificateAsync(
        Guid enrollmentId,
        string callerIdentityUserId,
        bool isEmployee,
        CancellationToken cancellationToken = default);
}

public interface IReportService
{
    Task<IReadOnlyList<TopWorkshopReportDto>> GetTopWorkshopsAsync(CancellationToken cancellationToken = default);
}
