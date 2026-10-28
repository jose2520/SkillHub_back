namespace SkillHub.Application.DTOs;

public sealed record TopWorkshopReportDto(
    Guid WorkshopId,
    string Code,
    string Title,
    string InstructorName,
    string InstructorEmail,
    DateTime StartDateTime,
    DateTime EndDateTime,
    int Capacity,
    int ActiveEnrollments,
    int AvailableSpots);

public sealed record ExcelFileDto(
    byte[] Content,
    string FileName,
    string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
