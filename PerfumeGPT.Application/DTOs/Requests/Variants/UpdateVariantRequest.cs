using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public class UpdateVariantRequest
	{
		public string Sku { get; set; } = null!;
		public int VolumeMl { get; set; } // (30ml / 50ml / 100ml / etc.)
		public int ConcentrationId { get; set; } // (Eau de Parfum / Eau de Toilette / etc.)
		public VariantType Type { get; set; }
		public decimal BasePrice { get; set; }
		public VariantStatus Status { get; set; }

		// Upload First Pattern: Multiple images management
		public List<Guid>? MediaIdsToDelete { get; set; }
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
	}
}


