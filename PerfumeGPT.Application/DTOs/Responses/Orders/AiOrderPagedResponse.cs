namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
    /// <summary>
    /// Paged response cho AI backend qua NATS khi lấy danh sách đơn hàng
    /// </summary>
    public record AiOrderPagedResponse
    {
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages { get; init; }
        public List<AiOrderListItemResponse> Items { get; init; } = [];
    }
}
