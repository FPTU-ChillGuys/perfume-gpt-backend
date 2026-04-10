namespace PerfumeGPT.Application.DTOs.Requests.Users
{
	public record UpdateUserBasicInfoRequest
	{
		public required string FullName { get; init; }
		public required string PhoneNumber { get; init; }
	}
}
