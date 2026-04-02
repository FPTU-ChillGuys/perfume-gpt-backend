namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
	public record CreateAttributeRequest
	{
		public string? InternalCode { get; init; }
		public required string Name { get; init; }
		public string? Description { get; init; }
		public bool IsVariantLevel { get; init; }
	}
}
