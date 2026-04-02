using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductFastLookResponse
	{
		public Guid Id { get; init; }
		public required string Name { get; init; }
		public string? Description { get; init; }
		public required string BrandName { get; init; }
		public Gender Gender { get; init; }
		public required List<VariantFastLookResponse> Variants { get; init; }
		public int Rating { get; init; }
		public int ReviewCount { get; init; }
	}
}
