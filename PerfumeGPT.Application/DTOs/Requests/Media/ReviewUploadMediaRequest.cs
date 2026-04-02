using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record ReviewUploadMediaRequest
	{
		public required List<IFormFile> Images { get; init; }
	}
}
