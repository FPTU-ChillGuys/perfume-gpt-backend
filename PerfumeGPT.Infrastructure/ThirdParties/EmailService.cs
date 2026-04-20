using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class EmailService(IConfiguration configuration) : IEmailService
	{
		public async Task SendEmailAsync(string to, string subject, string htmlBody)
		{
			var email = new MimeMessage();
			email.From.Add(new MailboxAddress("PerfumeGPT", configuration["EmailAccount:SmtpUser"] ?? throw new ArgumentNullException("Thiếu cấu hình EmailAccount:SmtpUser")));
			email.To.Add(MailboxAddress.Parse(to));
			email.Subject = subject;
			email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

			using var smtp = new SmtpClient();
			await smtp.ConnectAsync(
				configuration["EmailSettings:SmtpServer"] ?? throw new ArgumentNullException("Thiếu cấu hình EmailSettings:SmtpServer"),
				int.Parse(configuration["EmailSettings:SmtpPort"] ?? throw new ArgumentNullException("Thiếu cấu hình EmailSettings:SmtpPort")),
				MailKit.Security.SecureSocketOptions.StartTls
			);
			await smtp.AuthenticateAsync(
				configuration["EmailAccount:SmtpUser"] ?? throw new ArgumentNullException("Thiếu cấu hình EmailAccount:SmtpUser"),
				configuration["EmailAccount:SmtpPass"] ?? throw new ArgumentNullException("Thiếu cấu hình EmailAccount:SmtpPass")
			);
			await smtp.SendAsync(email);
			await smtp.DisconnectAsync(true);
		}
	}
}
