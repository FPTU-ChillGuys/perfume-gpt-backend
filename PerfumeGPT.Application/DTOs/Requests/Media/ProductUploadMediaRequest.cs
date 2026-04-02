using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record ProductUploadMediaRequest
	{
		public required List<ProductImageUploadItem> Images { get; init; }
	}

	public record ProductImageUploadItem
	{
		public required IFormFile ImageFile { get; init; }
		public string? AltText { get; init; }
		public int DisplayOrder { get; init; } = 0;
		public bool IsPrimary { get; init; } = false;
	}
}
