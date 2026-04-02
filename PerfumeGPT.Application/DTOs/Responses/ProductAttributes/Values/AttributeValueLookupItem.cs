namespace PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values
{
	public record AttributeValueLookupItem
	{
		public int Id { get; init; }
		public required string Value { get; init; }
	}
}
