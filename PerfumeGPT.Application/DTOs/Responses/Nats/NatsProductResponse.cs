namespace PerfumeGPT.Application.DTOs.Responses.Nats;

/// <summary>
/// Response cho AI backend qua NATS - Product Media
/// </summary>
public sealed record NatsProductMediaResponse
{
	public required string Id { get; init; }
	public required string Url { get; init; }
	public string? ThumbnailUrl { get; init; }
	public required string Type { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Product Variant
/// </summary>
public sealed record NatsProductVariantResponse
{
	public required string Id { get; init; }
	public required string Sku { get; init; }
	public required int VolumeMl { get; init; }
	public required string ConcentrationName { get; init; }
	public required string Type { get; init; }
	public required decimal BasePrice { get; init; }
	public decimal? RetailPrice { get; init; }
	public required int StockQuantity { get; init; }
	public required string ProductName { get; init; }
	public required List<NatsProductMediaResponse> Media { get; init; }
	public string? CampaignName { get; init; }
	public string? VoucherCode { get; init; }
	public decimal? DiscountedPrice { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Product Attribute
/// </summary>
public sealed record NatsProductAttributeResponse
{
	public required int AttributeId { get; init; }
	public required string Name { get; init; }
	public required string Value { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Olfactory Family
/// </summary>
public sealed record NatsProductOlfactoryFamilyResponse
{
	public required int OlfactoryFamilyId { get; init; }
	public required string Name { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Scent Note
/// </summary>
public sealed record NatsProductScentNoteResponse
{
	public required int NoteId { get; init; }
	public required string Name { get; init; }
	public required string NoteType { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Product
/// </summary>
public sealed record NatsProductResponse
{
	public required string Id { get; init; }
	public string? Name { get; init; }
	public required string Gender { get; init; }
	public required string Origin { get; init; }
	public required int ReleaseYear { get; init; }
	public required int BrandId { get; init; }
	public required string BrandName { get; init; }
	public required int CategoryId { get; init; }
	public required string CategoryName { get; init; }
	public string? Description { get; init; }
	public required int NumberOfVariants { get; init; }
	public required List<NatsProductMediaResponse> Media { get; init; }
	public required List<NatsProductVariantResponse> Variants { get; init; }
	public required List<NatsProductAttributeResponse> Attributes { get; init; }
	public required List<NatsProductOlfactoryFamilyResponse> OlfactoryFamilies { get; init; }
	public required List<NatsProductScentNoteResponse> ScentNotes { get; init; }
}

/// <summary>
/// Response cho AI backend qua NATS - Product By Ids
/// </summary>
public sealed record NatsProductByIdsResponse
{
	public required List<NatsProductResponse> Items { get; init; }
}
