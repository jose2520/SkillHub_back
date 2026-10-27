using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using SkillHub.Application.Exceptions;
using SkillHub.Domain.Exceptions;

namespace SkillHub.API.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, errorCode, errors) = MapException(exception);

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled server exception on {Method} {Path} (TraceId: {TraceId})",
                httpContext.Request.Method,
                httpContext.Request.Path,
                httpContext.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Handled domain/application exception ({StatusCode} {ErrorCode}) on {Method} {Path}: {Message}",
                (int)statusCode,
                errorCode,
                httpContext.Request.Method,
                httpContext.Request.Path,
                exception.Message);
        }

        var safeMessage = statusCode == HttpStatusCode.InternalServerError
            ? "An unexpected internal server error occurred. Please try again later."
            : exception.Message;

        httpContext.Response.StatusCode = (int)statusCode;
        httpContext.Response.ContentType = "application/problem+json; charset=utf-8";

        var payload = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            code = errorCode,
            message = safeMessage,
            detail = safeMessage,
            instance = httpContext.Request.Path.Value,
            traceId = httpContext.TraceIdentifier,
            errors,
            timestamp = DateTime.UtcNow
        };

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonOptions),
            cancellationToken);

        return true;
    }

    private static (HttpStatusCode StatusCode, string Title, string ErrorCode, object? Errors) MapException(Exception exception) =>
        exception switch
        {
            ValidationException valEx => (
                HttpStatusCode.BadRequest,
                "Validation Error",
                "Validation.Failed",
                valEx.Errors),

            InvalidDomainValueException domValEx => (
                HttpStatusCode.BadRequest,
                "Invalid Input Value",
                domValEx.ErrorCode,
                new Dictionary<string, string[]> { [domValEx.PropertyName] = [domValEx.Message] }),

            UnauthorizedException => (
                HttpStatusCode.Unauthorized,
                "Authentication Failed",
                "Auth.Unauthorized",
                null),

            ForbiddenAccessException => (
                HttpStatusCode.Forbidden,
                "Forbidden Access",
                "Auth.Forbidden",
                null),

            NotFoundException => (
                HttpStatusCode.NotFound,
                "Resource Not Found",
                "Resource.NotFound",
                null),

            CapacityExceededException capEx => (
                HttpStatusCode.Conflict,
                "Workshop Capacity Exceeded",
                capEx.ErrorCode,
                new { workshopCode = capEx.WorkshopCode, capacity = capEx.Capacity }),

            DuplicateEnrollmentException dupEx => (
                HttpStatusCode.Conflict,
                "Duplicate Active Enrollment",
                dupEx.ErrorCode,
                new { participantEmail = dupEx.ParticipantEmail, workshopCode = dupEx.WorkshopCode }),

            ScheduleConflictException schedEx => (
                HttpStatusCode.Conflict,
                "Workshop Schedule Conflict",
                schedEx.ErrorCode,
                new
                {
                    conflictingWorkshopCode = schedEx.ConflictingWorkshopCode,
                    conflictingStartUtc = schedEx.ConflictingStartUtc,
                    conflictingEndUtc = schedEx.ConflictingEndUtc
                }),

            ConflictException => (
                HttpStatusCode.Conflict,
                "Resource Conflict",
                "Resource.Conflict",
                null),

            BusinessRuleException ruleEx when ruleEx.ErrorCode is "Workshop.PastStartDate" or "Workshop.InvalidSchedule" or "Workshop.InvalidCapacity" => (
                HttpStatusCode.BadRequest,
                "Business Rule Validation Error",
                ruleEx.ErrorCode,
                null),

            BusinessRuleException ruleEx => (
                HttpStatusCode.Conflict,
                "Business Rule Violation",
                ruleEx.ErrorCode,
                null),

            DomainException domEx => (
                HttpStatusCode.BadRequest,
                "Domain Error",
                domEx.ErrorCode,
                null),

            _ => (
                HttpStatusCode.InternalServerError,
                "Internal Server Error",
                "Server.UnexpectedError",
                null)
        };
}
