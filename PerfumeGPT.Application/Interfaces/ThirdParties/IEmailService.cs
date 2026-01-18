namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
    }
}
