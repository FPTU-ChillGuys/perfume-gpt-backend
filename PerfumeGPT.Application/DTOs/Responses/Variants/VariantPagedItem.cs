using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantPagedItem
	{
		public Guid Id { get; set; }
		public Guid ProductId { get; set; }
		public MediaResponse? PrimaryImage { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }
		public int StockQuantity { get; set; }
		public List<ProductAttributeResponse>? Attributes { get; set; }
	}
}

