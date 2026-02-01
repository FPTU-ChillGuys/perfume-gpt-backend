using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	/// <summary>
	/// Request for uploading variant image (single image only)
	/// </summary>
	public class VariantUploadMediaRequest
	{
		public IFormFile ImageFile { get; set; } = null!;
	}
}
