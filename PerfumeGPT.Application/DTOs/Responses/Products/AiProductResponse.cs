namespace PerfumeGPT.Application.DTOs.Responses.Products
{
    /// <summary>
    /// Response cho AI backend qua NATS (kế thừa từ ProductResponse)
    /// Đảm bảo type safety và camelCase serialization
    /// </summary>
    public record AiProductResponse
    {
        public string Id { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string Gender { get; init; } = string.Empty;
        public string Origin { get; init; } = string.Empty;
        public int ReleaseYear { get; init; }
        public int BrandId { get; init; }
        public string BrandName { get; init; } = string.Empty;
        public int CategoryId { get; init; }
        public string CategoryName { get; init; } = string.Empty;
        public string? Description { get; init; }
        public int NumberOfVariants { get; init; }
        public List<AiProductMediaResponse> Media { get; init; } = [];
        public List<AiProductVariantResponse> Variants { get; init; } = [];
        public List<AiProductAttributeResponse> Attributes { get; init; } = [];
        public List<AiProductOlfactoryFamilyResponse> OlfactoryFamilies { get; init; } = [];
        public List<AiProductScentNoteResponse> ScentNotes { get; init; } = [];
    }

    public record AiProductVariantResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Sku { get; init; } = string.Empty;
        public int VolumeMl { get; init; }
        public string ConcentrationName { get; init; } = string.Empty;
        public string Type { get; init; } = "Standard";
        public decimal BasePrice { get; init; }
        public decimal? RetailPrice { get; init; }
        public int StockQuantity { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public List<AiProductMediaResponse> Media { get; init; } = [];
        public string? CampaignName { get; init; }
        public string? VoucherCode { get; init; }
        public decimal? DiscountedPrice { get; init; }
    }

    public record AiProductMediaResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Url { get; init; } = string.Empty;
        public string? ThumbnailUrl { get; init; }
        public string Type { get; init; } = "Image";
    }

    public record AiProductAttributeResponse
    {
        public int AttributeId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Value { get; init; } = string.Empty;
    }

    public record AiProductOlfactoryFamilyResponse
    {
        public int OlfactoryFamilyId { get; init; }
        public string Name { get; init; } = string.Empty;
    }

    public record AiProductScentNoteResponse
    {
        public int NoteId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string NoteType { get; init; } = string.Empty;
    }
}
