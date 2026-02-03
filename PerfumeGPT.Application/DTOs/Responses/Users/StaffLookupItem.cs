namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public class StaffLookupItem
	{
		public Guid Id { get; set; }
		public string UserName { get; set; } = string.Empty;
		public string FullName { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}
