using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record BannerUploadMediaRequest
	{
		public required List<IFormFile> Images { get; init; }
	}
}
