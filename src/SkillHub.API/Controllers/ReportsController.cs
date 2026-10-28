using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Enums;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// 12. Top workshops ordered by active enrollments [Employee]
    /// </summary>
    /// <remarks>Returns workshops ordered from highest to lowest number of active enrollments.</remarks>
    [HttpGet("top-workshops", Name = "12_GetTopWorkshops")]
    [Authorize(Roles = RoleNames.Employee)]
    [ProducesResponseType(typeof(IReadOnlyList<TopWorkshopReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TopWorkshopReportDto>>> GetTopWorkshops(
        CancellationToken cancellationToken)
    {
        var report = await _reportService.GetTopWorkshopsAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// 12b. Export top workshops report to branded Excel (.xlsx) [Employee]
    /// </summary>
    /// <remarks>Generates an executive SkillHub Excel workbook with embedded corporate logo, KPI cards, and occupancy dashboard.</remarks>
    [HttpGet("top-workshops/excel", Name = "12b_ExportTopWorkshopsExcel")]
    [Authorize(Roles = RoleNames.Employee)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ExportTopWorkshopsExcel(CancellationToken cancellationToken)
    {
        var excel = await _reportService.ExportTopWorkshopsExcelAsync(cancellationToken);
        return File(excel.Content, excel.ContentType, excel.FileName);
    }
}
