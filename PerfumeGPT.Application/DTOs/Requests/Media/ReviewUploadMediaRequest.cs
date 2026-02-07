using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public class ReviewUploadMediaRequest
	{
		public List<IFormFile> Images { get; set; } = [];
	}
}
