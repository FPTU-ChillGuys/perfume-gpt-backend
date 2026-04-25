namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
    /// <summary>
    /// Response tổng cho AI backend qua NATS khi lấy giỏ hàng
    /// Đảm bảo type safety và camelCase serialization
    /// </summary>
    public record AiCartResponse
    {
        public List<AiCartItemResponse> Items { get; init; } = [];
        public int TotalCount { get; init; }
        public decimal TotalAmount { get; init; }
        public decimal TotalDiscount { get; init; }
        public decimal FinalTotal { get; init; }
    }
}
