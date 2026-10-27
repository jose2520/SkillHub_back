using FluentValidation;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Entities;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Errors;
using SkillHub.Domain.ValueObjects;
using ValidationException = SkillHub.Application.Exceptions.ValidationException;

namespace SkillHub.Application.Services;

public sealed class WorkshopService : IWorkshopService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<CreateWorkshopRequestDto> _createValidator;

    public WorkshopService(
        IWorkshopRepository workshopRepository,
        IInstructorRepository instructorRepository,
        IEnrollmentRepository enrollmentRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IValidator<CreateWorkshopRequestDto> createValidator)
    {
        _workshopRepository = workshopRepository;
        _instructorRepository = instructorRepository;
        _enrollmentRepository = enrollmentRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<WorkshopResponseDto>> GetUpcomingWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var upcomingWorkshops = await _workshopRepository.GetUpcomingWithDetailsAsync(
            _dateTimeProvider.UtcNow,
            cancellationToken);

        return upcomingWorkshops
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<WorkshopResponseDto> GetWorkshopByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Workshop.NotFound(id.ToString()).Description);

        return MapToResponse(workshop);
    }

    public async Task<WorkshopResponseDto> CreateWorkshopAsync(
        CreateWorkshopRequestDto request,
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

        var normalizedCode = WorkshopCode.Create(request.Code).Value;
        if (await _workshopRepository.ExistsByCodeAsync(normalizedCode, cancellationToken))
            throw new ConflictException(DomainErrors.Workshop.DuplicateCode(normalizedCode).Description);

        var instructorEmail = EmailAddress.Create(request.InstructorEmail).Value;
        var instructor = await _instructorRepository.GetByEmailAsync(instructorEmail, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Instructor.NotFoundByEmail(instructorEmail).Description);

        var workshop = Workshop.CreateNew(
            normalizedCode,
            request.Title,
            request.Description,
            instructor.Id,
            request.StartDateTime,
            request.EndDateTime,
            request.Capacity,
            _dateTimeProvider.UtcNow);

        await _workshopRepository.AddAsync(workshop, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkshopResponseDto(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            instructor.Id,
            instructor.FullName,
            instructor.Email,
            instructor.Specialty,
            workshop.StartDateTime,
            workshop.EndDateTime,
            workshop.Capacity,
            ActiveEnrollments: 0,
            AvailableSpots: workshop.Capacity);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetWorkshopEnrollmentsAsync(
        Guid workshopId,
        CancellationToken cancellationToken = default)
    {
        var workshop = await _workshopRepository.GetByIdWithDetailsAsync(workshopId, cancellationToken)
            ?? throw new NotFoundException(DomainErrors.Workshop.NotFound(workshopId.ToString()).Description);

        var enrollments = await _enrollmentRepository.GetByWorkshopIdWithDetailsAsync(workshop.Id, cancellationToken);

        return enrollments
            .Select(EnrollmentService.MapToResponse)
            .ToList();
    }

    internal static WorkshopResponseDto MapToResponse(Workshop workshop)
    {
        var activeCount = workshop.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
        var instructor = workshop.Instructor;

        return new WorkshopResponseDto(
            workshop.Id,
            workshop.Code,
            workshop.Title,
            workshop.Description,
            workshop.InstructorId,
            instructor?.FullName ?? string.Empty,
            instructor?.Email ?? string.Empty,
            instructor?.Specialty ?? string.Empty,
            workshop.StartDateTime,
            workshop.EndDateTime,
            workshop.Capacity,
            activeCount,
            workshop.CalculateAvailableSpots(activeCount));
    }
}
