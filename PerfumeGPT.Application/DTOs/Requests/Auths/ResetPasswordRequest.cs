namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public class ResetPasswordRequest
	{
		public string Password { get; set; } = string.Empty;
		public string ConfirmPassword { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Token { get; set; } = string.Empty;
	}
}
