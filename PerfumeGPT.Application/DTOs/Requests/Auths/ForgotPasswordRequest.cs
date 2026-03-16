namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public class ForgotPasswordRequest
	{
		public string Email { get; set; } = null!;
		public string ClientUri { get; set; } = null!; //https://localhost:7011/api/auths/reset-password
	}
}
