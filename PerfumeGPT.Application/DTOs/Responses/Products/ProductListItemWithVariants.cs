using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductListItemWithVariants : ProductListItem
	{
		public List<VariantSummaryItem> Variants { get; set; } = [];
	}
}
