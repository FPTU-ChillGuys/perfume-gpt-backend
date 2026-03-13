using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Variants;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductResponse
	{
		public Guid Id { get; set; }
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public string BrandName { get; set; } = null!;
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = null!;
		public string? Description { get; set; }
		public int NumberOfVariants { get; set; }
		public List<MediaResponse> Media { get; set; } = [];
		public List<ProductVariantResponse> Variants { get; set; } = [];
		public List<ProductAttributeResponse> Attributes { get; set; } = [];
	}
}

