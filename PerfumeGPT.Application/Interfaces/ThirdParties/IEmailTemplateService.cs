namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
    public interface IEmailTemplateService
    {
        string GetRegisterTemplate(string username, string verifyUrl);
    }
}
