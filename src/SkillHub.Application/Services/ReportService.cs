using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Enums;

namespace SkillHub.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IWorkshopRepository _workshopRepository;

    public ReportService(IWorkshopRepository workshopRepository)
    {
        _workshopRepository = workshopRepository;
    }

    public async Task<IReadOnlyList<TopWorkshopReportDto>> GetTopWorkshopsAsync(CancellationToken cancellationToken = default)
    {
        var workshops = await _workshopRepository.GetAllWithDetailsAsync(cancellationToken);

        return workshops
            .Select(w =>
            {
                var activeCount = w.Enrollments.Count(e => e.Status == EnrollmentStatus.Active);
                return new TopWorkshopReportDto(
                    WorkshopId: w.Id,
                    Code: w.Code,
                    Title: w.Title,
                    InstructorName: w.Instructor?.FullName ?? string.Empty,
                    InstructorEmail: w.Instructor?.Email ?? string.Empty,
                    StartDateTime: w.StartDateTime,
                    EndDateTime: w.EndDateTime,
                    Capacity: w.Capacity,
                    ActiveEnrollments: activeCount,
                    AvailableSpots: w.CalculateAvailableSpots(activeCount));
            })
            .OrderByDescending(x => x.ActiveEnrollments)
            .ThenBy(x => x.Code)
            .ToList();
    }
}
