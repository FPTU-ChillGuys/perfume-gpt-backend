using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public record UpdateVariantRequest
	{
		public required string Sku { get; init; }
		public required string Barcode { get; init; }
		public int VolumeMl { get; init; }
		public int ConcentrationId { get; init; }
		public VariantType Type { get; init; }
		public decimal BasePrice { get; init; }
		public decimal? RetailPrice { get; init; }
		public VariantStatus Status { get; init; }
		public ReplenishmentPolicy RestockPolicy { get; init; }
		public int Sillage { get; init; }
		public int Longevity { get; init; }

		// Upload First Pattern: Multiple images management
		public List<Guid>? MediaIdsToDelete { get; init; }
		public List<Guid>? TemporaryMediaIdsToAdd { get; init; }

		// Attribute management for variants
		public List<ProductAttributeDto>? Attributes { get; init; }
	}
}


