using AlertHub.Application.Features.Notifications;
using AlertHub.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AlertHub.Infrastructure.Email;

/// <summary>
/// Transmits over SMTP. Deliberately knows nothing about redirection — that decision is
/// made by the decorator wrapped around this, so this class only ever sends where it is told.
/// </summary>
public class SmtpEmailSender(IOptions<EmailSettings> settings) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var options = settings.Value;
        var mime = new MimeMessage();

        mime.From.Add(new MailboxAddress(options.FromName, options.From));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;
        mime.Body = new TextPart("plain") { Text = message.Body };

        // Lets a redirected inbox be filtered and audited by real intended recipient.
        if (message.RedirectedFrom is { Length: > 0 } original)
            mime.Headers.Add("X-AlertHub-Original-To", original);

        using var client = new SmtpClient();

        await client.ConnectAsync(
            options.Smtp.Host,
            options.Smtp.Port,
            options.Smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
            ct);

        if (!string.IsNullOrWhiteSpace(options.Smtp.Username))
            await client.AuthenticateAsync(options.Smtp.Username, options.Smtp.Password, ct);

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);
    }
}
