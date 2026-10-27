using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Enums;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/workshops")]
public sealed class WorkshopsController : ControllerBase
{
    private readonly IWorkshopService _workshopService;

    public WorkshopsController(IWorkshopService workshopService)
    {
        _workshopService = workshopService;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<WorkshopResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkshopResponseDto>>> GetUpcomingWorkshops(
        CancellationToken cancellationToken)
    {
        var workshops = await _workshopService.GetUpcomingWorkshopsAsync(cancellationToken);
        return Ok(workshops);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WorkshopResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkshopResponseDto>> GetWorkshopById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        return Ok(workshop);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Employee)]
    [ProducesResponseType(typeof(WorkshopResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkshopResponseDto>> CreateWorkshop(
        [FromBody] CreateWorkshopRequestDto request,
        CancellationToken cancellationToken)
    {
        var created = await _workshopService.CreateWorkshopAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWorkshopById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/enrollments")]
    [Authorize(Roles = RoleNames.Employee)]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponseDto>>> GetWorkshopEnrollments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var enrollments = await _workshopService.GetWorkshopEnrollmentsAsync(id, cancellationToken);
        return Ok(enrollments);
    }
}
