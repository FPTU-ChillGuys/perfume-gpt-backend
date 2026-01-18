namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
    public class RegisterRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ClientUri { get; set; } = string.Empty; //https://localhost:7011/api/auths/verify-email
    }
}
