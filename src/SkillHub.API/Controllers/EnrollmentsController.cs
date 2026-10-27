using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkillHub.Application.DTOs;
using SkillHub.Application.Exceptions;
using SkillHub.Application.Interfaces;
using SkillHub.Domain.Enums;

namespace SkillHub.API.Controllers;

[ApiController]
[Route("api/v1/enrollments")]
public sealed class EnrollmentsController : ControllerBase
{
    private const long MaxExcelFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentResponseDto>> CreateEnrollment(
        [FromBody] CreateEnrollmentRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var enrollment = await _enrollmentService.EnrollAuthenticatedParticipantAsync(
            userId,
            request,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, enrollment);
    }

    [HttpGet("mine")]
    [Authorize(Roles = RoleNames.Participant)]
    [ProducesResponseType(typeof(IReadOnlyList<EnrollmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EnrollmentResponseDto>>> GetMyEnrollments(
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var enrollments = await _enrollmentService.GetMyEnrollmentsAsync(userId, cancellationToken);
        return Ok(enrollments);
    }

    [HttpPatch("{id:guid}/cancel")]
    [Authorize(Roles = $"{RoleNames.Participant},{RoleNames.Employee}")]
    [ProducesResponseType(typeof(EnrollmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrollmentResponseDto>> CancelEnrollment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var email = User.FindFirstValue(ClaimTypes.Email) ?? userId;
        var isEmployee = User.IsInRole(RoleNames.Employee);

        var updated = await _enrollmentService.CancelEnrollmentAsync(
            id,
            userId,
            email,
            isEmployee,
            cancellationToken);

        return Ok(updated);
    }

    [HttpPost("import")]
    [Authorize(Roles = RoleNames.Employee)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxExcelFileSizeBytes)]
    [ProducesResponseType(typeof(BulkImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<BulkImportResultDto>> ImportEnrollments(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("file", "A non-empty Excel (.xlsx) file is required.");

        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("file", "Only '.xlsx' Excel files are supported.");

        await using var stream = file.OpenReadStream();
        var result = await _enrollmentService.ImportEnrollmentsAsync(stream, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/certificate")]
    [Authorize(Roles = $"{RoleNames.Participant},{RoleNames.Employee}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DownloadCertificate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();
        var isEmployee = User.IsInRole(RoleNames.Employee);

        var certificate = await _enrollmentService.GenerateCertificateAsync(
            id,
            userId,
            isEmployee,
            cancellationToken);

        return File(certificate.Content, certificate.ContentType, certificate.FileName);
    }

    private string GetAuthenticatedUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException("User identifier claim is missing from the authentication token.");
}
