using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public record CreateProductRequest
	{
		public required string Name { get; init; }
		public int BrandId { get; init; }
		public int CategoryId { get; init; }
		public Gender Gender { get; init; }
		public required string Origin { get; init; }
		public int ReleaseYear { get; init; }
		public string? Description { get; init; }
		public required List<int> OlfactoryFamilyIds { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
		public required List<ScentNoteDto> ScentNotes { get; init; }
		public List<ProductAttributeDto>? Attributes { get; init; }
	}
}
