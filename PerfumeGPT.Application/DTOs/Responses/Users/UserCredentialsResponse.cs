namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public record UserCredentialsResponse
	{
		public Guid Id { get; init; }
		public int LoyaltyPoint { get; init; }
		public required string FullName { get; init; }
		public required string PhoneNumber { get; init; }
		public required string Email { get; init; }
		public string? ProfilePictureUrl { get; init; }
	}
}
