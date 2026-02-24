namespace PerfumeGPT.Application.DTOs.Requests.Auths
{
	public class LoginRequest
	{
		public string Credential { get; set; } = null!;
		public string Password { get; set; } = null!;
	}
}
