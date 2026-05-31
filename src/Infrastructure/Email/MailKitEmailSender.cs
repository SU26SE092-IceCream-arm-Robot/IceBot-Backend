using Application.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email
{
    public class MailKitEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<MailKitEmailSender> _logger;

        public MailKitEmailSender(IOptions<EmailOptions> options, ILogger<MailKitEmailSender> logger)
        {
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException("Recipient email is required.", nameof(to));
            }

            var message = new MimeMessage();
            var fromAddress = string.IsNullOrWhiteSpace(_options.From)
                ? throw new InvalidOperationException("Email 'From' address is not configured.")
                : _options.From;

            message.From.Add(new MailboxAddress(_options.DisplayName ?? _options.From, fromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody, TextBody = htmlBody }.ToMessageBody();

            try
            {
                using var client = new SmtpClient();
                var secureOption = _options.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                await client.ConnectAsync(_options.Host, _options.Port, secureOption, cancellationToken);

                if (!string.IsNullOrWhiteSpace(_options.UserName) &&
                    !string.IsNullOrWhiteSpace(_options.Password))
                {
                    await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);
                }

                await client.SendAsync(message, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", to);
                throw;
            }
        }
    }
}
