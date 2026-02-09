using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class UpdateProductRequest
	{
		// Product basic information
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public string? Description { get; set; }

		// Image management for updates
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
		public List<Guid>? MediaIdsToDelete { get; set; }

		// Attribute management for updates
		public List<ProductAttributeDto>? Attributes { get; set; }
	}
}
