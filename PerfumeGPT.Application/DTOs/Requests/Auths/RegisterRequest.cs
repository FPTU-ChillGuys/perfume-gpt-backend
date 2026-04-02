namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record RegisterRequest
	{
		public required string FullName { get; init; }
		public required string PhoneNumber { get; init; }
		public required string Email { get; init; }
		public required string Password { get; init; }
		public required string ClientUri { get; init; } //https://localhost:7011/api/auths/verify-email
	}
}