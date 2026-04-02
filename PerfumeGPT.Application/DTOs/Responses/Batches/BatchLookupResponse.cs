namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public record BatchLookupResponse
	{
		public Guid Id { get; init; }
		public required string BatchCode { get; init; }
		public Guid VariantId { get; init; }
		public required string Sku { get; init; }
	}
}
