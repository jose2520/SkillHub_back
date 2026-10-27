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

    [HttpGet("top-workshops")]
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
}
