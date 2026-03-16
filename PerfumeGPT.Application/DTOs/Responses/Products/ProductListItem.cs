using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductListItem
	{
		public Guid Id { get; set; }
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public string BrandName { get; set; } = null!;
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = null!;
		public string? Description { get; set; }
		public int NumberOfVariants { get; set; }
		public MediaResponse? PrimaryImage { get; set; }
		public List<ProductAttributeResponse>? Attributes { get; set; }
		public List<ProductScentNoteResponse> ScentNotes { get; set; } = [];
		public List<ProductOlfactoryFamilyResponse> OlfactoryFamilies { get; set; } = [];
	}
}


