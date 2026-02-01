using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	/// <summary>
	/// Request for uploading product images with metadata
	/// </summary>
	public class ProductUploadMediaRequest
	{
		public List<ProductImageUploadItem> Images { get; set; } = [];
	}

	public class ProductImageUploadItem
	{
		public IFormFile ImageFile { get; set; } = null!;
		public string? AltText { get; set; }
		public int DisplayOrder { get; set; } = 0;
		public bool IsPrimary { get; set; } = false;
	}
}
