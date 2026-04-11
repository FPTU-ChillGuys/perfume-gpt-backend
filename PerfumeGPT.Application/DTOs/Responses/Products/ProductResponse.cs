using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductScentNoteResponse
	{
		public int NoteId { get; init; }
		public required string Name { get; init; }
		public NoteType Type { get; init; }
	}

	public record ProductOlfactoryFamilyResponse
	{
		public int OlfactoryFamilyId { get; init; }
		public required string Name { get; init; }
	}

	public record ProductResponse
	{
		public Guid Id { get; init; }
		public string? Name { get; init; }
		public Gender Gender { get; init; }
		public required string Origin { get; init; }
		public int ReleaseYear { get; init; }
		public int BrandId { get; init; }
		public required string BrandName { get; init; }
		public int CategoryId { get; init; }
		public required string CategoryName { get; init; }
		public string? Description { get; init; }
		public int NumberOfVariants { get; init; }
		public required List<MediaResponse> Media { get; init; }
		public required List<ProductVariantResponse> Variants { get; init; }
		public required List<ProductAttributeResponse> Attributes { get; init; }

		public required List<ProductOlfactoryFamilyResponse> OlfactoryFamilies { get; init; }
		public required List<ProductScentNoteResponse> ScentNotes { get; init; }
	}

	public record PublicProductVariantResponse
	{
		public Guid Id { get; init; }
		public required string Sku { get; init; }
		public int VolumeMl { get; init; }
		public required string ConcentrationName { get; init; }
		public VariantType Type { get; init; }
		public decimal BasePrice { get; init; }
		public decimal? RetailPrice { get; init; }
		public int StockQuantity { get; init; }
		public required string ProductName { get; init; }
		public required List<MediaResponse> Media { get; init; }
		public string? CampaignName { get; init; }
		public string? VoucherCode { get; init; }
		public decimal? DiscountedPrice { get; init; }
	}

	public record PublicProductResponse
	{
		public Guid Id { get; init; }
		public string? Name { get; init; }
		public Gender Gender { get; init; }
		public required string Origin { get; init; }
		public int ReleaseYear { get; init; }
		public required string BrandName { get; init; }
		public required string CategoryName { get; init; }
		public string? Description { get; init; }
		public required List<MediaResponse> Media { get; init; }
		public required List<PublicProductVariantResponse> Variants { get; init; }
	}
}

