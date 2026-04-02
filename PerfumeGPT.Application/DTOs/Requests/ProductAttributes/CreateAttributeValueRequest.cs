namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
    public record CreateAttributeValueRequest
	{
		public required string Value { get; init; }
	}
}
