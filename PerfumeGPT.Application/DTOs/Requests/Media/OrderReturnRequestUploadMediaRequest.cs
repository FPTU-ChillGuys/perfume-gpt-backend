using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record OrderReturnRequestUploadMediaRequest
	{
		public List<IFormFile>? Videos { get; init; }
		public List<IFormFile>? Images { get; init; }
	}
}
