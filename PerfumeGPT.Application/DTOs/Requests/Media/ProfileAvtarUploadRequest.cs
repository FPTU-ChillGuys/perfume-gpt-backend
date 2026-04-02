using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record ProfileAvtarUploadRequest
	{
		public required IFormFile Avatar { get; init; }
		public string? AltText { get; init; }
	}
}
