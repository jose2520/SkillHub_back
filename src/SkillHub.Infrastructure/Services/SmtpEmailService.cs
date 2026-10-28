using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SkillHub.Application.Interfaces;
using SkillHub.Infrastructure.Configuration;

namespace SkillHub.Infrastructure.Services;

/// <summary>
/// Production-grade SMTP email delivery service utilizing MailKit.
/// Emits branded HTML notifications for participant welcome and enrollment confirmation.
/// Features resilient fault tolerance: if the SMTP server is unreachable or offline, the failure
/// is logged gracefully without aborting transactional business operations.
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpSettings> settings,
        ILogger<SmtpEmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(
        string recipientEmail,
        string recipientName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Bienvenido a SkillHub — Tu cuenta ha sido activada";

        var htmlBody = $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{subject}}</title>
          <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }
            .container { max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 12px; border: 1px solid #E2E8F0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }
            .header { background: linear-gradient(135deg, #1E1B4B 0%, #4F46E5 100%); padding: 32px 24px; text-align: center; color: #FFFFFF; }
            .header h1 { margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px; }
            .header p { margin: 6px 0 0; font-size: 14px; opacity: 0.9; }
            .content { padding: 32px 28px; line-height: 1.6; }
            .greeting { font-size: 18px; font-weight: 600; color: #0F172A; margin-bottom: 16px; }
            .badge { display: inline-block; background-color: #EEF2FF; color: #4F46E5; padding: 4px 12px; border-radius: 9999px; font-size: 13px; font-weight: 600; margin-bottom: 20px; }
            .card { background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 8px; padding: 20px; margin: 20px 0; }
            .card-title { font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #64748B; margin-bottom: 8px; }
            .footer { background: #F1F5F9; padding: 20px 24px; text-align: center; font-size: 12px; color: #64748B; border-top: 1px solid #E2E8F0; }
          </style>
        </head>
        <body>
          <div class="container">
            <div class="header">
              <h1>SkillHub</h1>
              <p>Plataforma de Capacitación y Gestión de Talento</p>
            </div>
            <div class="content">
              <span class="badge">Registro Exitoso</span>
              <div class="greeting">¡Hola, {{recipientName}}!</div>
              <p>Tu cuenta ha sido creada y configurada correctamente en <strong>SkillHub</strong>. Ahora puedes explorar el catálogo oficial de talleres corporativos y gestionar tus inscripciones.</p>
              
              <div class="card">
                <div class="card-title">Detalles de tu cuenta</div>
                <p style="margin: 4px 0;"><strong>Correo registrado:</strong> {{recipientEmail}}</p>
                <p style="margin: 4px 0;"><strong>Rol asignado:</strong> Participante (Participant)</p>
                <p style="margin: 4px 0;"><strong>Estado:</strong> Activo y Habilitado</p>
              </div>

              <p>Puedes acceder a la plataforma con tus credenciales para inscribirte a los talleres vigentes y descargar tus constancias oficiales de inscripción en formato PDF con radicado y código QR institucional.</p>
            </div>
            <div class="footer">
              <p style="margin: 0;">© {{DateTime.UtcNow.Year}} SkillHub Inc. Todos los derechos reservados.</p>
              <p style="margin: 4px 0 0;">Este es un mensaje institucional automático generado por la plataforma.</p>
            </div>
          </div>
        </body>
        </html>
        """;

        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
    }

    public async Task SendEnrollmentConfirmationAsync(
        string recipientEmail,
        string recipientName,
        string workshopTitle,
        string workshopCode,
        DateTime startUtc,
        DateTime endUtc,
        string instructorName,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Inscripción Confirmada: {workshopCode} — {workshopTitle}";

        var scheduleDisplay = $"{startUtc:dd/MM/yyyy HH:mm} - {endUtc:HH:mm} UTC";

        var htmlBody = $$"""
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1.0">
          <title>{{subject}}</title>
          <style>
            body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #F8FAFC; margin: 0; padding: 24px; color: #1E293B; }
            .container { max-width: 600px; margin: 0 auto; background: #FFFFFF; border-radius: 12px; border: 1px solid #E2E8F0; overflow: hidden; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05); }
            .header { background: linear-gradient(135deg, #1E1B4B 0%, #4F46E5 100%); padding: 32px 24px; text-align: center; color: #FFFFFF; }
            .header h1 { margin: 0; font-size: 26px; font-weight: 700; letter-spacing: -0.5px; }
            .header p { margin: 6px 0 0; font-size: 14px; opacity: 0.9; }
            .content { padding: 32px 28px; line-height: 1.6; }
            .greeting { font-size: 18px; font-weight: 600; color: #0F172A; margin-bottom: 16px; }
            .badge { display: inline-block; background-color: #ECFDF5; color: #059669; padding: 4px 12px; border-radius: 9999px; font-size: 13px; font-weight: 600; margin-bottom: 20px; }
            .card { background: #F8FAFC; border: 1px solid #E2E8F0; border-left: 4px solid #4F46E5; border-radius: 8px; padding: 20px; margin: 20px 0; }
            .card-title { font-size: 14px; font-weight: 600; text-transform: uppercase; letter-spacing: 0.5px; color: #4F46E5; margin-bottom: 8px; }
            .footer { background: #F1F5F9; padding: 20px 24px; text-align: center; font-size: 12px; color: #64748B; border-top: 1px solid #E2E8F0; }
          </style>
        </head>
        <body>
          <div class="container">
            <div class="header">
              <h1>SkillHub</h1>
              <p>Confirmación Oficial de Inscripción</p>
            </div>
            <div class="content">
              <span class="badge">Inscripción Exitosa</span>
              <div class="greeting">Estimado(a) {{recipientName}},</div>
              <p>Tu inscripción al taller corporativo ha sido procesada y confirmada en el sistema de manera exitosa:</p>
              
              <div class="card">
                <div class="card-title">Detalles del Taller</div>
                <p style="margin: 4px 0;"><strong>Código:</strong> {{workshopCode}}</p>
                <p style="margin: 4px 0;"><strong>Taller:</strong> {{workshopTitle}}</p>
                <p style="margin: 4px 0;"><strong>Horario programado:</strong> {{scheduleDisplay}}</p>
                <p style="margin: 4px 0;"><strong>Instructor asignado:</strong> {{instructorName}}</p>
                <p style="margin: 4px 0;"><strong>Modalidad:</strong> Presencial / Sede Corporativa</p>
              </div>

              <p>Recuerda que desde el portal de SkillHub puedes descargar en cualquier momento tu <strong>Constancia de Inscripción en PDF</strong> con sello digital y código QR de verificación.</p>
            </div>
            <div class="footer">
              <p style="margin: 0;">© {{DateTime.UtcNow.Year}} SkillHub Inc. Todos los derechos reservados.</p>
              <p style="margin: 4px 0 0;">Este es un mensaje institucional automático generado por la plataforma.</p>
            </div>
          </div>
        </body>
        </html>
        """;

        await SendEmailAsync(recipientEmail, recipientName, subject, htmlBody, cancellationToken);
    }

    private async Task SendEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(new MailboxAddress(recipientName, recipientEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            // Short timeout so business transactions never block if server is unreachable
            client.Timeout = 5000;

            var socketOptions = _settings.EnableSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.UserName) && !string.IsNullOrWhiteSpace(_settings.Password))
            {
                await client.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(
                "Successfully dispatched email '{Subject}' to {RecipientEmail} via {Host}:{Port}.",
                subject, recipientEmail, _settings.Host, _settings.Port);
        }
        catch (Exception ex)
        {
            // Resilient fallback: log warning and continue without breaking the calling transaction
            _logger.LogWarning(
                ex,
                "SMTP email delivery failed for recipient {RecipientEmail} via {Host}:{Port}. Transaction proceeded successfully.",
                recipientEmail, _settings.Host, _settings.Port);
        }
    }
}
