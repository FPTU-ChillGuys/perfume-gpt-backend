namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public record GoogleLoginRequest
	{
		public required string IdToken { get; init; }

		public string? FcmToken { get; init; }
		public string? DeviceType { get; init; }
	}
}
