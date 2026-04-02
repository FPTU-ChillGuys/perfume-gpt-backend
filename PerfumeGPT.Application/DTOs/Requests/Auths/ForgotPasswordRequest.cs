namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record ForgotPasswordRequest
	{
		public required string Email { get; init; }
		public required string ClientUri { get; init; } //https://localhost:7011/api/auths/reset-password
	}
}
