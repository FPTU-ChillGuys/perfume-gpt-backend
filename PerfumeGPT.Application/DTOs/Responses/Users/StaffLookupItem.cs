namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public record StaffLookupItem
	{
		public Guid Id { get; init; }
		public required string UserName { get; init; }
		public required string FullName { get; init; }
		public required string Email { get; init; }
	}
}
