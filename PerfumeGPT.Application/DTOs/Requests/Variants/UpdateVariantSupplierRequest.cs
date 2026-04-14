namespace PerfumeGPT.Application.DTOs.Requests.Variants
{
	public record UpdateVariantSupplierRequest
	{
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
		public bool IsPrimary { get; init; }
	}
}
