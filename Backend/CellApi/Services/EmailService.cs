using CellApi.Models;
using CellApi.Repositories;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CellApi.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguracionRepository _configRepo;
    private readonly IEmailLogRepository      _logRepo;
    private readonly ILogger<EmailService>    _logger;

    public EmailService(
        IConfiguracionRepository configRepo,
        IEmailLogRepository      logRepo,
        ILogger<EmailService>    logger)
    {
        _configRepo = configRepo;
        _logRepo    = logRepo;
        _logger     = logger;
    }

    public async Task SendAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        string tipo,
        string referenciaTipo,
        int    referenciaId,
        byte[]? adjuntoPdf    = null,
        string? nombreAdjunto = null)
    {
        // Leer configuración SMTP en tiempo real desde la BD
        var cfg = await _configRepo.GetAllAsync();

        var smtpHost     = cfg.GetValueOrDefault("smtp_host",       "smtp.gmail.com");
        var smtpPuerto   = int.TryParse(cfg.GetValueOrDefault("smtp_puerto", "587"), out var p) ? p : 587;
        var smtpSsl      = cfg.GetValueOrDefault("smtp_ssl",  "true")
                              .Equals("true", StringComparison.OrdinalIgnoreCase);
        var smtpUsuario  = cfg.GetValueOrDefault("smtp_usuario",    "");
        var smtpPassword = cfg.GetValueOrDefault("smtp_password",   "");
        var fromName     = cfg.GetValueOrDefault("smtp_from_name",  "CellShop");
        var fromEmail    = cfg.GetValueOrDefault("smtp_from_email", smtpUsuario);

        // Registrar intento
        var log = new EmailLog
        {
            Destinatario   = destinatario,
            Asunto         = asunto,
            Cuerpo         = cuerpo,
            Tipo           = tipo,
            ReferenciaTipo = referenciaTipo,
            ReferenciaId   = referenciaId,
            Estado         = "pendiente",
            Intentos       = 0
        };
        var logId = await _logRepo.CreateAsync(log);

        try
        {
            if (string.IsNullOrWhiteSpace(smtpUsuario) || string.IsNullOrWhiteSpace(smtpPassword))
                throw new InvalidOperationException("SMTP no configurado. Ve a Configuración → Empresa y configura el correo saliente.");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(destinatario));
            message.Subject = asunto;

            var bodyBuilder = new BodyBuilder { HtmlBody = cuerpo };
            if (adjuntoPdf != null && !string.IsNullOrEmpty(nombreAdjunto))
                bodyBuilder.Attachments.Add(nombreAdjunto, adjuntoPdf,
                    new ContentType("application", "pdf"));
            message.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            var socketOptions = smtpSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await smtp.ConnectAsync(smtpHost, smtpPuerto, socketOptions);
            await smtp.AuthenticateAsync(smtpUsuario, smtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            await _logRepo.UpdateEstadoAsync(logId, "enviado", null, DateTime.UtcNow);
            _logger.LogInformation("Email enviado a {Dest} — {Asunto}", destinatario, asunto);
        }
        catch (Exception ex)
        {
            await _logRepo.UpdateEstadoAsync(logId, "error", ex.Message, null);
            _logger.LogError(ex, "Error enviando email a {Dest}", destinatario);
        }
    }
}
