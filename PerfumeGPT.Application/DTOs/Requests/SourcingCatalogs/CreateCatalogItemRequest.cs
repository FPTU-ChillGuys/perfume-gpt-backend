namespace PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs
{
	public record CreateCatalogItemRequest
	{
		public Guid ProductVariantId { get; init; }
		public int SupplierId { get; init; }
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
		public bool IsPrimary { get; init; }
	}
}
