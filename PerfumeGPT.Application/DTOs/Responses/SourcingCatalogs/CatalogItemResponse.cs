namespace PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs
{
	public record CatalogItemResponse
	{
		public Guid Id { get; init; }
		public Guid ProductVariantId { get; init; }
		public int SupplierId { get; init; }
		public required string SupplierName { get; init; }
		public required string VariantSku { get; init; }
		public decimal NegotiatedPrice { get; init; }
		public int EstimatedLeadTimeDays { get; init; }
		public bool IsPrimary { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}
}
