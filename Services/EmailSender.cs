using AuthApi.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AuthApi.Services;
public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    public EmailSender(IOptions<SmtpOptions> opt) => _opt = opt.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_opt.Host, _opt.Port, SecureSocketOptions.StartTlsWhenAvailable);
        if (!string.IsNullOrWhiteSpace(_opt.User))
            await client.AuthenticateAsync(_opt.User, _opt.Pass);
        await client.SendAsync(msg);
        await client.DisconnectAsync(true);
    }
}
