namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public record StaffManageItem
	{
		public Guid Id { get; init; }
		public required string UserName { get; init; }
		public required string FullName { get; init; }
		public required string Email { get; init; }
		public required string PhoneNumber { get; init; }
		public bool IsActive { get; init; }
		public string? ProfileImageUrl { get; init; }
	}
}
