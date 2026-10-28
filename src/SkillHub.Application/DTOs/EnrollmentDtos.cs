namespace SkillHub.Application.DTOs;

public sealed record CreateEnrollmentRequestDto(Guid WorkshopId);

public sealed record EnrollmentResponseDto(
    Guid Id,
    Guid ParticipantId,
    string ParticipantName,
    string ParticipantEmail,
    Guid WorkshopId,
    string WorkshopCode,
    string WorkshopTitle,
    string InstructorName,
    DateTime WorkshopStartDateTime,
    DateTime WorkshopEndDateTime,
    string Status,
    DateTime EnrolledAt,
    DateTime? CancelledAt,
    string? CancelledBy);

public sealed record ExcelEnrollmentRowDto(
    int RowNumber,
    string ParticipantEmail,
    string WorkshopCode);

public sealed record RejectedImportRowDto(
    int RowNumber,
    string ParticipantEmail,
    string WorkshopCode,
    string Reason);

public sealed record ApprovedImportRowDto(
    int RowNumber,
    string ParticipantEmail,
    string WorkshopCode);

public sealed record BulkImportResultDto(
    int EnrolledCount,
    int RejectedCount,
    int TotalProcessed,
    IReadOnlyList<RejectedImportRowDto> RejectedRows,
    IReadOnlyList<ApprovedImportRowDto>? ApprovedRows = null);

public sealed record CertificateDataDto(
    Guid EnrollmentId,
    string ParticipantFullName,
    string ParticipantEmail,
    string WorkshopCode,
    string WorkshopTitle,
    string InstructorFullName,
    DateTime WorkshopStartDateTime,
    DateTime WorkshopEndDateTime,
    DateTime EnrolledAtUtc,
    DateTime GeneratedAtUtc);

public sealed record CertificateFileDto(
    byte[] Content,
    string FileName,
    string ContentType);
