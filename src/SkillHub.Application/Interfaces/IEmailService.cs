namespace SkillHub.Application.Interfaces;

/// <summary>
/// Service abstraction for delivering transactional emails (welcome notifications, enrollment confirmations).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a branded welcome email to newly self-registered participants.
    /// </summary>
    Task SendWelcomeEmailAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an enrollment confirmation email with workshop schedule and instructor details.
    /// </summary>
    Task SendEnrollmentConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string workshopTitle,
        string workshopCode,
        DateTime startUtc,
        DateTime endUtc,
        string instructorName,
        CancellationToken cancellationToken = default);
}
