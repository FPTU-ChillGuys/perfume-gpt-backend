namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
    /// <summary>
    /// Response cho AI backend qua NATS khi lấy danh sách đơn hàng
    /// Đảm bảo type safety và camelCase serialization
    /// </summary>
    public record AiOrderListItemResponse
    {
        public string CreatedAt { get; init; } = string.Empty;
        public string? CustomerId { get; init; }
        public string? CustomerName { get; init; }
        public string Id { get; init; } = string.Empty;
        public string Code { get; init; } = string.Empty;
        public int ItemCount { get; init; }
        public string PaymentStatus { get; init; } = string.Empty;
        public int? ShippingStatus { get; init; }
        public string? StaffId { get; init; }
        public string? StaffName { get; init; }
        public string Status { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string Type { get; init; } = string.Empty;
        public string? UpdatedAt { get; init; }
    }
}
