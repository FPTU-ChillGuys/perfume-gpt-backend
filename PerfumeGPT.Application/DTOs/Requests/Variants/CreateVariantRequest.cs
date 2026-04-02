using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public record CreateVariantRequest
	{
		public Guid ProductId { get; init; }
		public required string Barcode { get; init; }
		public required string Sku { get; init; }
		public int VolumeMl { get; init; }
		public int ConcentrationId { get; init; }
		public int Sillage { get; init; }
		public int Longevity { get; init; }
		public VariantType Type { get; init; }
		public decimal BasePrice { get; init; }
		public decimal? RetailPrice { get; init; }
		public VariantStatus Status { get; init; }
		public int LowStockThreshold { get; init; }
		public List<ProductAttributeDto>? Attributes { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
	}
}

