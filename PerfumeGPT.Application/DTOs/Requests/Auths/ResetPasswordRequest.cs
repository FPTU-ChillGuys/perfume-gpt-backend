namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record ResetPasswordRequest
	{
		public required string Password { get; init; }
		public required string ConfirmPassword { get; init; }
		public required string Email { get; init; }
		public required string Token { get; init; }
	}
}
