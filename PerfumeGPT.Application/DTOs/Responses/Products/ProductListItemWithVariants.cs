using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductListItemWithVariants : ProductListItem
	{
		public required List<VariantSummaryItem> Variants { get; init; }
	}
}
