using MathTutor.Application.Abstractions;
using MathTutor.Domain.Entities;
using MathTutor.Infrastructure.Persistence;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MathTutor.Infrastructure.Services;

public sealed class EmailLogService(MathTutorDbContext dbContext, IOptions<EmailOptions> options) : IEmailService
{
    public async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var log = new EmailLog
        {
            ToEmail = toEmail.Trim(),
            Subject = subject.Trim(),
            Body = body.Trim(),
            Status = settings.Enabled ? "Pending" : "Skipped",
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.EmailLogs.Add(log);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (!settings.Enabled || !settings.UseSmtp)
        {
            log.Status = settings.Enabled ? "Logged" : "Skipped";
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromName, settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(log.ToEmail));
            message.Subject = log.Subject;
            message.Body = new TextPart("plain") { Text = log.Body };

            using var client = new SmtpClient();
            await client.ConnectAsync(settings.Host, settings.Port, settings.UseSsl, cancellationToken);
            if (!string.IsNullOrWhiteSpace(settings.User))
            {
                await client.AuthenticateAsync(settings.User, settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            log.Status = "Sent";
        }
        catch (Exception ex)
        {
            log.Status = "Failed";
            log.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
