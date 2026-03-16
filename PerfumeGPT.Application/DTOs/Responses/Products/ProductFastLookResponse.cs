using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductFastLookResponse
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = null!;
		public string? Description { get; set; }
		public string BrandName { get; set; } = null!;
		public Gender Gender { get; set; }
		public List<VariantFastLookResponse> Variants { get; set; } = [];
		public int Rating { get; set; }
		public int ReviewCount { get; set; }
	}
}
