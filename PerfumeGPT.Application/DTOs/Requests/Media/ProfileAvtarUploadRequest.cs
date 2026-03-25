using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class ProfileAvtarUploadRequest
	{
		public IFormFile Avatar { get; set; } = null!;
		public string? AltText { get; set; }
	}
}
