using FluentValidation;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Errors;
using SkillHub.Domain.Exceptions;
using SkillHub.Domain.ValueObjects;
using ValidationException = SkillHub.Application.Exceptions.ValidationException;

namespace SkillHub.Application.Services;

public sealed class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IParticipantRepository _participantRepository;
    private readonly IExcelImportService _excelImportService;
    private readonly ICertificatePdfService _certificatePdfService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateEnrollmentRequestDto> _createValidator;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepository,
        IWorkshopRepository workshopRepository,
        IParticipantRepository participantRepository,
        IExcelImportService excelImportService,
        ICertificatePdfService certificatePdfService,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateEnrollmentRequestDto> createValidator)
    {
        _enrollmentRepository = enrollmentRepository;
        _workshopRepository = workshopRepository;
        _participantRepository = participantRepository;
        _excelImportService = excelImportService;
        _certificatePdfService = certificatePdfService;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
    }

    public async Task<EnrollmentResponseDto> EnrollAuthenticatedParticipantAsync(
        string identityUserId,
        CreateEnrollmentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }

        var participant = await _participantRepository.GetByIdentityUserIdAsync(identityUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("Only registered participants can enroll in workshops.");

        var workshop = await _workshopRepository.GetByIdWithDetailsAsync(request.WorkshopId, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Workshop.NotFound(request.WorkshopId.ToString()).Description);

        var nowUtc = _dateTimeProvider.UtcNow;
        var enrollment = await CreateValidatedEnrollmentCoreAsync(participant, workshop, nowUtc, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return BuildEnrollmentResponse(enrollment, participant, workshop);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetMyEnrollmentsAsync(
        string identityUserId,
        CancellationToken cancellationToken = default)
    {
        var participant = await _participantRepository.GetByIdentityUserIdAsync(identityUserId, cancellationToken)
            ?? throw new ForbiddenAccessException("Participant profile was not found for the authenticated user.");

        var enrollments = await _enrollmentRepository.GetByParticipantIdWithDetailsAsync(
            participant.Id,
            cancellationToken);

        return enrollments
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<EnrollmentResponseDto> CancelEnrollmentAsync(
        Guid enrollmentId,
        string callerIdentityUserId,
        string callerEmail,
        bool isEmployee,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdWithDetailsAsync(enrollmentId, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Enrollment.NotFound(enrollmentId).Description);

        var participant = enrollment.Participant
            ?? throw new NotFoundException("Associated participant profile was not found.");

        var workshop = enrollment.Workshop
            ?? throw new NotFoundException("Associated workshop was not found.");

        if (!isEmployee && !string.Equals(participant.IdentityUserId, callerIdentityUserId, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException("Participants can only cancel their own enrollments.");
        }

        var actor = string.IsNullOrWhiteSpace(callerEmail) ? callerIdentityUserId : callerEmail;
        enrollment.Cancel(actor, isEmployee, workshop.StartDateTime, _dateTimeProvider.UtcNow);

        _enrollmentRepository.Update(enrollment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponse(enrollment);
    }

    public async Task<BulkImportResultDto> ImportEnrollmentsAsync(
        Stream excelStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        var rows = _excelImportService.ParseEnrollmentRows(excelStream);
        var rejectedRows = new List<RejectedImportRowDto>();
        var enrolledCount = 0;

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.ParticipantEmail))
            {
                rejectedRows.Add(new RejectedImportRowDto(
                    row.RowNumber,
                    row.ParticipantEmail,
                    row.WorkshopCode,
                    "ParticipantEmail is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.WorkshopCode))
            {
                rejectedRows.Add(new RejectedImportRowDto(
                    row.RowNumber,
                    row.ParticipantEmail,
                    row.WorkshopCode,
                    "WorkshopCode is required."));
                continue;
            }

            try
            {
                var normalizedEmail = EmailAddress.Create(row.ParticipantEmail).Value;
                var normalizedCode = WorkshopCode.Create(row.WorkshopCode).Value;

                var participant = await _participantRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
                if (participant is null)
                {
                    rejectedRows.Add(new RejectedImportRowDto(
                        row.RowNumber,
                        row.ParticipantEmail,
                        row.WorkshopCode,
                        DomainErrors.Participant.NotFoundByEmail(normalizedEmail).Description));
                    continue;
                }

                var workshop = await _workshopRepository.GetByCodeAsync(normalizedCode, cancellationToken);
                if (workshop is null)
                {
                    rejectedRows.Add(new RejectedImportRowDto(
                        row.RowNumber,
                        row.ParticipantEmail,
                        row.WorkshopCode,
                        DomainErrors.Workshop.NotFound(normalizedCode).Description));
                    continue;
                }

                await CreateValidatedEnrollmentCoreAsync(
                    participant,
                    workshop,
                    _dateTimeProvider.UtcNow,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                enrolledCount++;
            }
            catch (DomainException ex)
            {
                rejectedRows.Add(new RejectedImportRowDto(
                    row.RowNumber,
                    row.ParticipantEmail,
                    row.WorkshopCode,
                    ex.Message));
            }
        }

        return new BulkImportResultDto(
            EnrolledCount: enrolledCount,
            RejectedCount: rejectedRows.Count,
            TotalProcessed: rows.Count,
            RejectedRows: rejectedRows);
    }

    public async Task<CertificateFileDto> GenerateCertificateAsync(
        Guid enrollmentId,
        string callerIdentityUserId,
        bool isEmployee,
        CancellationToken cancellationToken = default)
    {
        var enrollment = await _enrollmentRepository.GetByIdWithDetailsAsync(enrollmentId, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Enrollment.NotFound(enrollmentId).Description);

        var participant = enrollment.Participant
            ?? throw new NotFoundException("Associated participant profile was not found.");

        var workshop = enrollment.Workshop
            ?? throw new NotFoundException("Associated workshop was not found.");

        if (!isEmployee && !string.Equals(participant.IdentityUserId, callerIdentityUserId, StringComparison.Ordinal))
        {
            throw new ForbiddenAccessException("Participants can only download certificates for their own enrollments.");
        }

        enrollment.EnsureCanGenerateCertificate();

        var certificateData = new CertificateDataDto(
            EnrollmentId: enrollment.Id,
            ParticipantFullName: participant.FullName,
            ParticipantEmail: participant.Email,
            WorkshopCode: workshop.Code,
            WorkshopTitle: workshop.Title,
            InstructorFullName: workshop.Instructor?.FullName ?? "SkillHub Instructor",
            WorkshopStartDateTime: workshop.StartDateTime,
            WorkshopEndDateTime: workshop.EndDateTime,
            EnrolledAtUtc: enrollment.EnrolledAt,
            GeneratedAtUtc: _dateTimeProvider.UtcNow);

        var pdfBytes = _certificatePdfService.GenerateEnrollmentCertificate(certificateData);
        var fileName = $"SkillHub_Certificate_{workshop.Code}_{enrollment.Id:N}.pdf";

        return new CertificateFileDto(pdfBytes, fileName, "application/pdf");
    }

    private async Task<Enrollment> CreateValidatedEnrollmentCoreAsync(
        Participant participant,
        Workshop workshop,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        // Rule 2: No active duplicates (cancelled enrollments allow re-enrollment per Rule 6)
        if (await _enrollmentRepository.HasActiveEnrollmentAsync(participant.Id, workshop.Id, cancellationToken))
            throw new DuplicateEnrollmentException(participant.Email, workshop.Code);

        // Rule 4 (future workshop only) & Rule 1 (capacity counts active enrollments only)
        var activeWorkshopEnrollments = await _enrollmentRepository.CountActiveByWorkshopIdAsync(
            workshop.Id,
            cancellationToken);
        workshop.EnsureCanAcceptEnrollment(activeWorkshopEnrollments, nowUtc);

        // Rule 3: No schedule overlap with any other active enrollment of the same participant
        var targetSlot = workshop.GetTimeSlot();
        var activeParticipantEnrollments = await _enrollmentRepository.GetActiveByParticipantIdWithWorkshopAsync(
            participant.Id,
            cancellationToken);

        foreach (var existing in activeParticipantEnrollments)
        {
            if (existing.Workshop is null)
                continue;

            var existingSlot = existing.Workshop.GetTimeSlot();
            if (targetSlot.OverlapsWith(existingSlot))
            {
                throw new ScheduleConflictException(
                    existing.Workshop.Code,
                    existing.Workshop.StartDateTime,
                    existing.Workshop.EndDateTime);
            }
        }

        var enrollment = Enrollment.Create(participant.Id, workshop.Id, nowUtc);
        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
        return enrollment;
    }

    internal static EnrollmentResponseDto MapToResponse(Enrollment enrollment)
    {
        var participant = enrollment.Participant;
        var workshop = enrollment.Workshop;

        return new EnrollmentResponseDto(
            enrollment.Id,
            enrollment.ParticipantId,
            participant?.FullName ?? string.Empty,
            participant?.Email ?? string.Empty,
            enrollment.WorkshopId,
            workshop?.Code ?? string.Empty,
            workshop?.Title ?? string.Empty,
            workshop?.Instructor?.FullName ?? string.Empty,
            workshop?.StartDateTime ?? DateTime.MinValue,
            workshop?.EndDateTime ?? DateTime.MinValue,
            enrollment.Status.ToString(),
            enrollment.EnrolledAt,
            enrollment.CancelledAt,
            enrollment.CancelledBy);
    }

    private static EnrollmentResponseDto BuildEnrollmentResponse(
        Enrollment enrollment,
        Participant participant,
        Workshop workshop) =>
        new(
            enrollment.Id,
            participant.Id,
            participant.FullName,
            participant.Email,
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Instructor?.FullName ?? string.Empty,
            workshop.StartDateTime,
            workshop.EndDateTime,
            enrollment.Status.ToString(),
            enrollment.EnrolledAt,
            enrollment.CancelledAt,
            enrollment.CancelledBy);
}
