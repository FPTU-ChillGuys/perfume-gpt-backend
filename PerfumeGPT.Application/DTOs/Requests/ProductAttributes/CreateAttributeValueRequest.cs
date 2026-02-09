namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
    public class CreateAttributeValueRequest
    {
        public int AttributeId { get; set; }
        public string Value { get; set; } = null!;
    }
}
