namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantSummaryItem
	{
		public Guid Id { get; init; }
		public required string DisplayName { get; init; }
		public required string ConcentrationName { get; init; }
	}
}
