namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantSupplierResponse
	{
		public int SupplierId { get; init; }
		public required string SupplierName { get; init; }
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
		public bool IsPrimary { get; init; }
	}
}
