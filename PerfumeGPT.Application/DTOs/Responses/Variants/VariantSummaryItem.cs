namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantSummaryItem
	{
		public Guid Id { get; set; }
		public string DisplayName { get; set; } = null!;
		public string ConcentrationName { get; set; } = null!;
	}
}
