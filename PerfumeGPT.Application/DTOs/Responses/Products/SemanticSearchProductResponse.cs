using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
    public class SemanticSearchProductResponse : ProductListItemWithVariants
    {
        public List<string> ScentNotes { get; set; } = [];
        public List<string> OlfactoryFamilies { get; set; } = [];
    }
}
