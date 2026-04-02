using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductInforResponse
	{
		public required string ProductCode { get; init; }
		public required string BrandName { get; init; }
		public required string Origin { get; init; }
		public int ReleaseYear { get; init; }
		public Gender Gender { get; init; }
		public required string ScentGroup { get; init; }
		public required string Style { get; init; }
		public required string TopNotes { get; init; }
		public required string HeartNotes { get; init; }
		public required string BaseNotes { get; init; }
		public required string Description { get; init; }
	}
}
