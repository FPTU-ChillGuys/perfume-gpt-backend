using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class CreateVariantRequest
	{
		public Guid ProductId { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public int Sillage { get; set; }
		public int Longevity { get; set; }
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }
		public int LowStockThreshold { get; set; }
		public List<ProductAttributeDto>? Attributes { get; set; }
		public List<Guid>? TemporaryMediaIds { get; set; }
	}
}

