namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public record UserLookupItem
	{
		public Guid Id { get; init; }
		public required string FullName { get; init; }
		public required string Email { get; init; }
	}
}
