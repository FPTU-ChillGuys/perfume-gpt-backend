using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantFastLookResponse
	{
		public Guid Id { get; set; }
		public string DisplayName { get; set; } = null!;
		public decimal Price { get; set; }
		public decimal? RetailPrice { get; set; }
		public int StockQuantity { get; set; }
		public MediaResponse? Media { get; set; }
	}
}
