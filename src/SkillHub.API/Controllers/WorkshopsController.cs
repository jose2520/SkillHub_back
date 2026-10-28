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

    /// <summary>
    /// 03. List upcoming workshops with available spots [Public]
    /// </summary>
    /// <remarks>Lists future workshops that have not started yet along with their calculated available spots.</remarks>
    [HttpGet(Name = "03_GetUpcomingWorkshops")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<WorkshopResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WorkshopResponseDto>>> GetUpcomingWorkshops(
        CancellationToken cancellationToken)
    {
        var workshops = await _workshopService.GetUpcomingWorkshopsAsync(cancellationToken);
        return Ok(workshops);
    }

    /// <summary>
    /// 04. Get workshop detail with available spots [Public]
    /// </summary>
    /// <remarks>Returns the details of a single workshop along with its calculated available spots.</remarks>
    [HttpGet("{id}", Name = "04_GetWorkshopById")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(WorkshopResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkshopResponseDto>> GetWorkshopById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var workshop = await _workshopService.GetWorkshopByIdAsync(id, cancellationToken);
        return Ok(workshop);
    }

    /// <summary>
    /// 05. Create a future workshop [Employee]
    /// </summary>
    /// <remarks>Publishes a new future workshop linked to a registered instructor by email.</remarks>
    [HttpPost(Name = "05_CreateWorkshop")]
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

    /// <summary>
    /// 06. List enrollments of a workshop [Employee]
    /// </summary>
    /// <remarks>Lists all participants enrolled in the specified workshop.</remarks>
    [HttpGet("{id}/enrollments", Name = "06_GetWorkshopEnrollments")]
    [Authorize(Roles = RoleNames.Employee)]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
