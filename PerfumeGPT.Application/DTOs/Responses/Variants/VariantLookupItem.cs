namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantLookupItem
	{
		public Guid Id { get; init; }
		public required string Barcode { get; init; }
		public required string Sku { get; init; }
		public required string DisplayName { get; init; }
		public int VolumeMl { get; init; }
		public required string ConcentrationName { get; init; }
		public decimal BasePrice { get; init; }
		public string? PrimaryImageUrl { get; init; }
	}
}

