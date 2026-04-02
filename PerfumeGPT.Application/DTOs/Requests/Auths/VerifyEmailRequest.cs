namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record VerifyEmailRequest
	{
		public required string Email { get; init; }
		public required string Token { get; init; }
	}
}
