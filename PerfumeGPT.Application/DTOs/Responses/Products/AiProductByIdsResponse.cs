namespace PerfumeGPT.Application.DTOs.Responses.Products
{
    /// <summary>
    /// Response cho AI backend qua NATS khi lấy products theo IDs
    /// </summary>
    public record AiProductByIdsResponse
    {
        public List<AiProductResponse> Items { get; init; } = [];
    }
}
