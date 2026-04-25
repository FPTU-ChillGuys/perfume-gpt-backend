namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
    /// <summary>
    /// Response cho AI backend qua NATS khi thực hiện mutation trên giỏ hàng (add/update/remove/clear)
    /// </summary>
    public record AiCartMutationResponse
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? Message { get; init; }
        public AiCartItemResponse? Item { get; init; }
    }
}
