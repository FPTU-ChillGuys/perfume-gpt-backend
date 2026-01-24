namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public class VariantLookupItem
	{
		public Guid Id { get; set; }
		public string Sku { get; set; } = null!;
		public string DisplayName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public decimal BasePrice { get; set; }
		public string? ImageUrl { get; set; }
	}
}
