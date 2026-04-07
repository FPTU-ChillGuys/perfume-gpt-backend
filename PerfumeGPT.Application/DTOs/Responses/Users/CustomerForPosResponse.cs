namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public record CustomerForPosResponse
	{
		public Guid Id { get; init; }
		public required string FullName { get; init; }
		public required string PhoneNumber { get; init; }
		public required string Email { get; init; }
		public int LoyaltyPoint { get; init; }
	}
}
