using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class CreateVariantRequest
	{
		public Guid ProductId { get; set; }
		public string Barcode { get; set; } = null!;
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; } // (30ml / 50ml / 100ml / etc.)
		public int ConcentrationId { get; set; } // (Eau de Parfum / Eau de Toilette / etc.)
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }

		// Attribute management for variants
		public List<ProductAttributeDto>? Attributes { get; set; }

		// Upload First Pattern: Multiple images uploaded to temporary storage first
		public List<Guid>? TemporaryMediaIds { get; set; }
	}
}

