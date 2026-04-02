namespace PerfumeGPT.Application.DTOs.Responses.Variants
{
	public record VariantCreateOrder
	{
		public Guid Id { get; init; }
		public decimal UnitPrice { get; init; }
		public required string Snapshot { get; init; }
	}
}
