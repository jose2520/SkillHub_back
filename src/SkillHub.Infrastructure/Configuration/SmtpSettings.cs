namespace SkillHub.Infrastructure.Configuration;

/// <summary>
/// Strongly typed configuration model for outgoing SMTP email delivery.
/// </summary>
public sealed class SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public bool EnableSsl { get; set; } = false;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = "no-reply@skillhub.internal";
    public string FromName { get; set; } = "SkillHub Platform";
}
