using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
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
