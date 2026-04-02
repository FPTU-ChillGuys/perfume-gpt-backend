using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantFastLookResponse
	{
		public Guid Id { get; init; }
		public required string Sku { get; init; }
		public required string DisplayName { get; init; }
		public decimal Price { get; init; }
		public decimal? RetailPrice { get; init; }
		public int StockQuantity { get; init; }
		public MediaResponse? Media { get; init; }
	}
}
