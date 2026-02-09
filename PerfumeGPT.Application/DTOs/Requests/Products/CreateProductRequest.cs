using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class CreateProductRequest
	{
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public string? Description { get; set; }

		// Upload First Pattern: Images uploaded to temporary storage first
		public List<Guid>? TemporaryMediaIds { get; set; }

		// Product attributes
		public List<ProductAttributeDto>? Attributes { get; set; }
	}
}
