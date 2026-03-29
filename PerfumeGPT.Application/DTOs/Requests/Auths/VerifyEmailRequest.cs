namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public class VerifyEmailRequest
	{
		public string Email { get; set; } = null!;
		public string Token { get; set; } = null!;
	}
}
