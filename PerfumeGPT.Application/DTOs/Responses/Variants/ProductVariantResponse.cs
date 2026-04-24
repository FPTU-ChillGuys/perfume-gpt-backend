using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record ProductVariantResponse
	{
		public Guid Id { get; init; }
		public required string Barcode { get; init; }
		public required string Sku { get; init; }
		public int VolumeMl { get; init; }
		public int ConcentrationId { get; init; }
		public required string ConcentrationName { get; init; }
		public VariantType Type { get; init; }
		public decimal BasePrice { get; init; }
		public decimal? RetailPrice { get; init; }
		public VariantStatus Status { get; init; }
		public int StockQuantity { get; init; }
		public int Sillage { get; init; }
		public int Longevity { get; init; }

		// Product details
		public Guid ProductId { get; init; }
		public required string ProductName { get; init; }

		// Media
		public required List<MediaResponse> Media { get; init; }

		// Campaign details 
		public string? CampaignName { get; init; }
		public int PromotionalStockQuantity { get; init; }      // THÊM MỚI: Tồn kho được giảm giá
		public decimal? DiscountedPrice { get; init; }

		// Attributes
		public List<ProductAttributeResponse>? Attributes { get; init; }
		public List<VariantSupplierResponse>? Suppliers { get; init; }
	}
}

