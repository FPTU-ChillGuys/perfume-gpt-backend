namespace PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs
{
	public record UpdateCatalogItemRequest
	{
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
	}
}
