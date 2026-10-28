using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SkillHub.Infrastructure.Configuration;
using SkillHub.Infrastructure.Services;
using Xunit;

namespace SkillHub.Tests.Infrastructure;

public sealed class SmtpEmailServiceTests
{
    [Fact]
    public async Task SendWelcomeEmailAsync_WhenSmtpServerUnavailable_LogsWarningAndDoesNotThrow()
    {
        var settings = Options.Create(new SmtpSettings
        {
            Host = "127.0.0.1",
            Port = 25255, // non-existent port to guarantee connection refused
            EnableSsl = false,
            FromEmail = "no-reply@skillhub.internal",
            FromName = "SkillHub Platform"
        });

        var loggerMock = new Mock<ILogger<SmtpEmailService>>();
        var sut = new SmtpEmailService(settings, loggerMock.Object);

        // Act & Assert: Must complete without throwing any exception
        await sut.SendWelcomeEmailAsync(
            "test.user@correo.com",
            "Test User",
            CancellationToken.None);

        // Verify that a warning was logged indicating failure was caught gracefully
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task SendEnrollmentConfirmationAsync_WhenSmtpServerUnavailable_LogsWarningAndDoesNotThrow()
    {
        var settings = Options.Create(new SmtpSettings
        {
            Host = "127.0.0.1",
            Port = 25255,
            EnableSsl = false,
            FromEmail = "no-reply@skillhub.internal",
            FromName = "SkillHub Platform"
        });

        var loggerMock = new Mock<ILogger<SmtpEmailService>>();
        var sut = new SmtpEmailService(settings, loggerMock.Object);

        // Act & Assert
        await sut.SendEnrollmentConfirmationAsync(
            "test.user@correo.com",
            "Test User",
            "Advanced Cloud Architecture",
            "CLD-101",
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddHours(4),
            "Dr. Alan Turing",
            CancellationToken.None);

        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
