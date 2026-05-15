using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using MailKit.Net.Smtp;
using MimeKit;

namespace HospitalWebApp.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Read SMTP settings from configuration.
        var host = _config["EmailSettings:Host"];
        var port = int.Parse(_config["EmailSettings:Port"] ?? "587");
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var password = _config["EmailSettings:Password"];
        var senderName = _config["EmailSettings:SenderName"];

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException("Email settings are not configured.");
        }

        try 
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, password);

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName ?? "Hospital", senderEmail));
                message.To.Add(new MailboxAddress("", email));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlMessage };

                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                
                // Log success for local debugging.
                System.Diagnostics.Debug.WriteLine($"Email sent successfully to {email}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ CRITICAL EMAIL FAILURE: {ex.Message}");
            throw;
        }
    }
}