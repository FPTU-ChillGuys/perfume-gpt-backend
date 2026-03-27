using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
    public class SemanticSearchProductResponse : ProductListItemWithVariants
    {
        public string? Gender { get; set; }
        public string? Origin { get; set; }
        public int? ReleaseYear { get; set; }
        public List<string> Attributes { get; set; } = [];
        public List<string> ScentNotes { get; set; } = [];
        public List<string> OlfactoryFamilies { get; set; } = [];
    }
}
