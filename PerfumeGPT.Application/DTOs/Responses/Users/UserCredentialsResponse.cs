namespace PerfumeGPT.Application.DTOs.Responses.Users
{
	public class UserCredentialsResponse
	{
		public Guid Id { get; set; }
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}
