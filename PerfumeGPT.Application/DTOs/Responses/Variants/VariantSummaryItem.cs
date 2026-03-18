using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantSummaryItem
	{
		public Guid Id { get; set; }
		public string DisplayName { get; set; } = null!;
		public string ConcentrationName { get; set; } = null!;
		public MediaResponse? PrimaryImage { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }
		public int StockQuantity { get; set; }
		public List<ProductAttributeResponse>? Attributes { get; set; }
	}
}
