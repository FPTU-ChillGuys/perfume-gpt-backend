using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class ProductVariantResponse
	{
		public Guid Id { get; set; }
		public Guid ProductId { get; set; }
		public string ProductName { get; set; } = null!;
		public List<MediaResponse> Media { get; set; } = [];
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
		public List<ProductAttributeResponse>? Attributes { get; set; }
	}
}

