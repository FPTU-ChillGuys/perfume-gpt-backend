namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Order Detail Item
/// </summary>
public sealed record NatsOrderDetailResponse
{
	public required string VariantName { get; init; }
	public required int Quantity { get; init; }
	public required decimal UnitPrice { get; init; }
	public required decimal Total { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Order List Item
/// </summary>
public sealed record NatsOrderListItemResponse
{
	public required string CreatedAt { get; init; }
	public string? CustomerId { get; init; }
	public string? CustomerName { get; init; }
	public required string Id { get; init; }
	public required string Code { get; init; }
	public required int ItemCount { get; init; }
	public required string PaymentStatus { get; init; }
	public string? ShippingStatus { get; init; }
	public string? StaffId { get; init; }
	public string? StaffName { get; init; }
	public required string Status { get; init; }
	public required decimal TotalAmount { get; init; }
	public required string Type { get; init; }
	public string? UpdatedAt { get; init; }
	public List<NatsOrderDetailResponse> OrderDetails { get; init; } = [];
}

/// <summary>
/// Response cho AI backend qua NATS - Order Paged
/// </summary>
public sealed record NatsOrderPagedResponse
{
	public required int TotalCount { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public required int TotalPages { get; init; }
	public required List<NatsOrderListItemResponse> Items { get; init; }
}
