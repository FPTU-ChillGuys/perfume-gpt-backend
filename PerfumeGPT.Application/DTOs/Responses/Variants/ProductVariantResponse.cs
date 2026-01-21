using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class ProductVariantResponse
	{
		public Guid Id { get; set; }
		public Guid ProductId { get; set; }
		public string? ImageUrl { get; set; }
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; } // (30ml / 50ml / 100ml / etc.)
		public int ConcentrationId { get; set; } // (Eau de Parfum / Eau de Toilette / etc.)
		public string ConcentrationName { get; set; } = null!;
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }
	}
}
