using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantPagedItem
	{
		public Guid Id { get; init; }
		public Guid ProductId { get; init; }
		public string? PrimaryImageUrl { get; init; }
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
		public List<ProductAttributeResponse>? Attributes { get; init; }
	}
}

