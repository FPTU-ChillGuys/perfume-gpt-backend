namespace PerfumeGPT.Application.DTOs.Requests.ProductAttributes
{
    public class CreateAttributeRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsVariantLevel { get; set; }
    }
}
