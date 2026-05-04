namespace PerfumeGPT.Application.DTOs.Requests.Users
{
	public record GetUserLookupRequest
	{
		public string? FullName { get; init; }
		public string? Email { get; init; }
		public string? PhoneNumber { get; init; }
	}
}
