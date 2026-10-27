namespace SkillHub.Application.DTOs;

public sealed record CreateWorkshopRequestDto(
    string Code,
    string Title,
    string Description,
    string InstructorEmail,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int Capacity);

public sealed record WorkshopResponseDto(
    Guid Id,
    string Code,
    string Title,
    string Description,
    Guid InstructorId,
    string InstructorName,
    string InstructorEmail,
    string InstructorSpecialty,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int Capacity,
    int ActiveEnrollments,
    int AvailableSpots);
