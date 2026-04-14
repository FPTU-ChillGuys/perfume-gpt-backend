namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public record CreateVariantSupplierRequest
	{
		public int SupplierId { get; init; }
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
		public bool IsPrimary { get; init; }
	}
}
