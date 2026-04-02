using Microsoft.AspNetCore.Http;

namespace PerfumeGPT.Application.DTOs.Requests.Media
{
	public record VariantUploadMediaRequest
	{
		public required List<VariantImageUploadItem> Images { get; init; }
	}

	public record VariantImageUploadItem
	{
		public required IFormFile ImageFile { get; init; }
		public string? AltText { get; init; }
		public int DisplayOrder { get; init; } = 0;
		public bool IsPrimary { get; init; } = false;
	}
}
