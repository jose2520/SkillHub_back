using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SkillHub.API.Controllers;
using SkillHub.API.Middleware;
using SkillHub.Application.Exceptions;
using SkillHub.Domain.Enums;
using SkillHub.Domain.Exceptions;

namespace SkillHub.Tests.API;

public sealed class GlobalExceptionHandlerAndSecurityTests
{
    public static TheoryData<Exception, int, string> ExceptionMappings => new()
    {
        { new ValidationException("email", "Invalid email"), 400, "Validation.Failed" },
        { new InvalidDomainValueException("Code", "Invalid code"), 400, "Validation.Code" },
        { new BusinessRuleException("Workshop.PastStartDate", "Must be future"), 400, "Workshop.PastStartDate" },
        { new UnauthorizedException("Invalid credentials"), 401, "Auth.Unauthorized" },
        { new ForbiddenAccessException("Not owner"), 403, "Auth.Forbidden" },
        { new NotFoundException("Workshop not found"), 404, "Resource.NotFound" },
        { new CapacityExceededException("FOT-001", 15), 409, "Workshop.CapacityExceeded" },
        { new DuplicateEnrollmentException("a@b.co", "FOT-001"), 409, "Enrollment.DuplicateActive" },
        { new ScheduleConflictException("COC-001", DateTime.UtcNow, DateTime.UtcNow.AddHours(2)), 409, "Enrollment.ScheduleConflict" },
        { new ConflictException("Duplicate code"), 409, "Resource.Conflict" },
        { new BusinessRuleException("Enrollment.CancellationWindowClosed", "Under 24h"), 409, "Enrollment.CancellationWindowClosed" },
        { new InvalidOperationException("Sensitive internal DB detail"), 500, "Server.UnexpectedError" }
    };

    [Theory]
    [MemberData(nameof(ExceptionMappings))]
    public async Task GlobalExceptionHandler_MapsExceptionsToExpectedHttpStatusAndSafePayload(
        Exception exception,
        int expectedStatusCode,
        string expectedErrorCode)
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/enrollments";
        using var responseStream = new MemoryStream();
        context.Response.Body = responseStream;

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);

        responseStream.Position = 0;
        using var doc = await JsonDocument.ParseAsync(responseStream);
        Assert.Equal(expectedStatusCode, doc.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedErrorCode, doc.RootElement.GetProperty("code").GetString());

        // Verify OWASP rule: 500 errors must never leak raw internal exception messages
        if (expectedStatusCode == 500)
        {
            var message = doc.RootElement.GetProperty("message").GetString()!;
            Assert.DoesNotContain("Sensitive internal DB detail", message);
        }
    }

    [Fact]
    public void Controllers_EnforceExpectedRoleAuthorizationAttributesAcrossAll12Endpoints()
    {
        // Public endpoints
        AssertHasAttribute<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Register));
        AssertHasAttribute<AllowAnonymousAttribute>(typeof(AuthController), nameof(AuthController.Login));
        AssertHasAttribute<AllowAnonymousAttribute>(typeof(WorkshopsController), nameof(WorkshopsController.GetUpcomingWorkshops));
        AssertHasAttribute<AllowAnonymousAttribute>(typeof(WorkshopsController), nameof(WorkshopsController.GetWorkshopById));

        // Employee-only endpoints
        AssertAuthorizeRoles(typeof(WorkshopsController), nameof(WorkshopsController.CreateWorkshop), RoleNames.Employee);
        AssertAuthorizeRoles(typeof(WorkshopsController), nameof(WorkshopsController.GetWorkshopEnrollments), RoleNames.Employee);
        AssertAuthorizeRoles(typeof(EnrollmentsController), nameof(EnrollmentsController.ImportEnrollments), RoleNames.Employee);
        AssertAuthorizeRoles(typeof(ReportsController), nameof(ReportsController.GetTopWorkshops), RoleNames.Employee);

        // Participant-only endpoints
        AssertAuthorizeRoles(typeof(EnrollmentsController), nameof(EnrollmentsController.CreateEnrollment), RoleNames.Participant);
        AssertAuthorizeRoles(typeof(EnrollmentsController), nameof(EnrollmentsController.GetMyEnrollments), RoleNames.Participant);

        // Dual-role (Owner Participant or Employee) endpoints
        AssertAuthorizeRoles(
            typeof(EnrollmentsController),
            nameof(EnrollmentsController.CancelEnrollment),
            $"{RoleNames.Participant},{RoleNames.Employee}");
        AssertAuthorizeRoles(
            typeof(EnrollmentsController),
            nameof(EnrollmentsController.DownloadCertificate),
            $"{RoleNames.Participant},{RoleNames.Employee}");
    }

    private static void AssertHasAttribute<TAttr>(Type controllerType, string methodName) where TAttr : Attribute
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttribute<TAttr>());
    }

    private static void AssertAuthorizeRoles(Type controllerType, string methodName, string expectedRoles)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);
        var authAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal(expectedRoles, authAttr!.Roles);
    }
}
