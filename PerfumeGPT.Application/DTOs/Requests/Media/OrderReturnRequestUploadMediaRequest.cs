using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record OrderReturnRequestUploadMediaRequest
	{
		public required List<IFormFile> Videos { get; init; }
	}
}
