using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	/// <summary>
	/// Request for uploading variant images with metadata (supports multiple images)
	/// </summary>
	public class VariantUploadMediaRequest
	{
		public List<VariantImageUploadItem> Images { get; set; } = [];
	}

	public class VariantImageUploadItem
	{
		public IFormFile ImageFile { get; set; } = null!;
		public string? AltText { get; set; }
		public int DisplayOrder { get; set; } = 0;
		public bool IsPrimary { get; set; } = false;
	}
}
