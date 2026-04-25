namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Cart Item
/// </summary>
public sealed record NatsCartItemResponse
{
	public required string CartItemId { get; init; }
	public required string VariantId { get; init; }
	public required string VariantName { get; init; }
	public required string ImageUrl { get; init; }
	public required int VolumeMl { get; init; }
	public required string Type { get; init; }
	public required decimal VariantPrice { get; init; }
	public required int Quantity { get; init; }
	public required bool IsAvailable { get; init; }
	public required decimal SubTotal { get; init; }
	public required int PromotionalQuantity { get; init; }
	public required int RegularQuantity { get; init; }
	public required decimal Discount { get; init; }
	public required decimal FinalTotal { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Cart
/// </summary>
public sealed record NatsCartResponse
{
	public required List<NatsCartItemResponse> Items { get; init; }
	public required int TotalCount { get; init; }
	public required decimal TotalAmount { get; init; }
	public required decimal TotalDiscount { get; init; }
	public required decimal FinalTotal { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Cart Mutation
/// </summary>
public sealed record NatsCartMutationResponse
{
	public required bool Success { get; init; }
	public string? Error { get; init; }
	public string? Message { get; init; }
	public NatsCartItemResponse? Item { get; init; }
}
