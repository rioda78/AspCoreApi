using AspCoreApi.Helpers;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Options;

namespace AspCoreApi.Services;

public interface IEmailServis
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}

public class EmailKirim : IEmailServis
{

    private readonly EmailSettings _settings;

    public EmailKirim(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;

        email.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = body
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, _settings.UseSSL);
        await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    
    }
}