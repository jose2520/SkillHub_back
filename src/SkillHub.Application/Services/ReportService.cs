using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Abstractions;
using SkillHub.Domain.Enums;

namespace SkillHub.Application.Services;

public sealed class ReportService : IReportService
{
    private readonly IWorkshopRepository _workshopRepository;
    private readonly IExcelReportService _excelReportService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ReportService(
        IWorkshopRepository workshopRepository,
        IExcelReportService excelReportService,
        IDateTimeProvider dateTimeProvider)
    {
        _workshopRepository = workshopRepository;
        _excelReportService = excelReportService;
        _dateTimeProvider = dateTimeProvider;
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

    public async Task<ExcelFileDto> ExportTopWorkshopsExcelAsync(CancellationToken cancellationToken = default)
    {
        var topWorkshops = await GetTopWorkshopsAsync(cancellationToken);
        var nowUtc = _dateTimeProvider.UtcNow;
        var bytes = _excelReportService.GenerateTopWorkshopsExcelReport(topWorkshops, nowUtc);
        var fileName = $"SkillHub_TopWorkshops_{nowUtc:yyyyMMdd_HHmm}.xlsx";
        return new ExcelFileDto(bytes, fileName);
    }

    public async Task<ExcelFileDto> GenerateEnrollmentTemplateExcelAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = _dateTimeProvider.UtcNow;
        var upcoming = await _workshopRepository.GetUpcomingWithDetailsAsync(nowUtc, cancellationToken);
        var mapped = upcoming.Select(WorkshopService.MapToResponse).ToList();
        var bytes = _excelReportService.GenerateEnrollmentImportTemplate(mapped, nowUtc);
        return new ExcelFileDto(bytes, "SkillHub_Enrollments_Template.xlsx");
    }
}
