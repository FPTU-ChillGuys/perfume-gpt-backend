using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantSummaryItem
	{
		public Guid Id { get; set; }

		/// <summary>
		/// e.g. "Eau de Parfum - 50ml"
		/// </summary>
		public string DisplayName { get; set; } = null!;

		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public decimal BasePrice { get; set; }
		public VariantType Type { get; set; }
		public VariantStatus Status { get; set; }
	}
}
