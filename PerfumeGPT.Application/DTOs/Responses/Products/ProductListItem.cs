using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductListItem
	{
		public Guid Id { get; init; }
		public string? Name { get; init; }
		public int BrandId { get; init; }
		public required string BrandName { get; init; }
		public int CategoryId { get; init; }
		public required string CategoryName { get; init; }
		public string? Description { get; init; }
		public int NumberOfVariants { get; init; }
		public required List<decimal> VariantPrices { get; init; }
		public List<string>? Tags { get; init; }
		public MediaResponse? PrimaryImage { get; init; }
	}
}


