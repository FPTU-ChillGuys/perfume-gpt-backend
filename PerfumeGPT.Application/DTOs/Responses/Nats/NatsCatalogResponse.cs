namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Catalog Item
/// </summary>
public sealed record NatsCatalogItemResponse
{
	public required string Id { get; init; }
	public required string VariantId { get; init; }
	public required int SupplierId { get; init; }
	public required string SupplierName { get; init; }
	public required decimal BasePrice { get; init; }
	public required int MinOrderQuantity { get; init; }
	public required int LeadTimeDays { get; init; }
	public required bool IsPrimary { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Catalog
/// </summary>
public sealed record NatsCatalogResponse
{
	public required List<NatsCatalogItemResponse> Catalogs { get; init; }
	public string? Error { get; init; }
}
