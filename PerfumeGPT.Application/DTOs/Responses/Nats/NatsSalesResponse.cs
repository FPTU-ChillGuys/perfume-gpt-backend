namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Daily Sales Record
/// </summary>
public sealed record NatsDailySalesRecord
{
	public required string Date { get; init; }
	public required int QuantitySold { get; init; }
	public required decimal Revenue { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Sales Analytics
/// </summary>
public sealed record NatsSalesAnalyticsResponse
{
	public required string VariantId { get; init; }
	public required int TotalQuantitySold { get; init; }
	public required decimal TotalRevenue { get; init; }
	public required double AverageDailySales { get; init; }
	public required int Last7DaysSales { get; init; }
	public required int Last30DaysSales { get; init; }
	public required string Trend { get; init; }
	public required string Volatility { get; init; }
	public required string Sku { get; init; }
	public required string ProductName { get; init; }
	public required int VolumeMl { get; init; }
	public required string Type { get; init; }
	public required decimal BasePrice { get; init; }
	public required string Status { get; init; }
	public required string ConcentrationName { get; init; }
	public required List<NatsDailySalesRecord> DailySalesData { get; init; }
}
