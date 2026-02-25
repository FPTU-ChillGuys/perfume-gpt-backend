using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	/// <summary>
	/// Extended version of <see cref="ProductListItem"/> that includes variant information
	/// (price, concentration, volume). Used by semantic search results so clients can
	/// display pricing without needing an extra round-trip.
	/// </summary>
	public class ProductListItemWithVariants : ProductListItem
	{
		public List<VariantSummaryItem> Variants { get; set; } = [];
	}
}
