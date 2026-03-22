using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class UpdateVariantRequest
	{
		public string Sku { get; set; } = null!;
		public string Barcode { get; set; } = null!;
		public int VolumeMl { get; set; }
		public int ConcentrationId { get; set; }
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public decimal? RetailPrice { get; set; }
		public VariantStatus Status { get; set; }
		public int Sillage { get; set; }
		public int Longevity { get; set; }

		// Upload First Pattern: Multiple images management
		public List<Guid>? MediaIdsToDelete { get; set; }
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }

		// Attribute management for variants
		public List<ProductAttributeDto>? Attributes { get; set; }
	}
}


