namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
    public record UpdateAttributeValueRequest
	{
		public required string Value { get; init; }
	}
}
