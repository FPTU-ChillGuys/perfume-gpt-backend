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
		public int FamilyId { get; set; }
		public string FamilyName { get; set; } = null!;
		public string? Description { get; set; }
		public string? TopNotes { get; set; }
		public string? MiddleNotes { get; set; }
		public string? BaseNotes { get; set; }
		public List<ProductVariantResponse> Variants { get; set; } = new();
	}
}
