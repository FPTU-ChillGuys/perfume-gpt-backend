using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class ProductVariantResponse
	{
		public Guid Id { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public decimal? RetailPrice { get; set; }
		public VariantStatus Status { get; set; }
		public int StockQuantity { get; set; }
		public int Sillage { get; set; }
		public int Longevity { get; set; }

		// Product details
		public Guid ProductId { get; set; }
		public string ProductName { get; set; } = null!;

		// Media
		public List<MediaResponse> Media { get; set; } = [];

		// Campaign details 
		public string? CampaignName { get; set; }
		public string? VoucherCode { get; set; }
		public decimal? DiscountedPrice { get; set; }

		// Attributes
		public List<ProductAttributeResponse>? Attributes { get; set; }
	}
}

