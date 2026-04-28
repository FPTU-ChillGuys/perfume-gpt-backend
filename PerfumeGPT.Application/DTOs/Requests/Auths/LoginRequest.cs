namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record LoginRequest
	{
		public required string Credential { get; init; }
		public required string Password { get; init; }

		public string? FcmToken { get; init; }
		public string? DeviceType { get; init; }
	}
}
