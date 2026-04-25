namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Inventory Stock
/// </summary>
public sealed record NatsInventoryStockResponse
{
	public required string ConcentrationName { get; init; }
	public required decimal BasePrice { get; init; }
	public required string Id { get; init; }
	public required bool IsLowStock { get; init; }
	public required int LowStockThreshold { get; init; }
	public required string ProductName { get; init; }
	public required int TotalQuantity { get; init; }
	public required string VariantId { get; init; }
	public required string VariantSku { get; init; }
	public required int VolumeMl { get; init; }
	public required int AvailableQuantity { get; init; }
	public required string VariantStatus { get; init; }
	public required string Status { get; init; }
	public required string Type { get; init; }
	public required int ReservedQuantity { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Inventory Paged
/// </summary>
public sealed record NatsInventoryPagedResponse
{
	public required int TotalCount { get; init; }
	public required int PageNumber { get; init; }
	public required int PageSize { get; init; }
	public required int TotalPages { get; init; }
	public required List<NatsInventoryStockResponse> Items { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Inventory Overall Stats
/// </summary>
public sealed record NatsInventoryOverallStats
{
	public required int TotalSku { get; init; }
	public required int LowStockSku { get; init; }
	public required int OutOfStockSku { get; init; }
	public required int ExpiredBatches { get; init; }
	public required int NearExpiryBatches { get; init; }
	public required int CriticalAlerts { get; init; }
}
