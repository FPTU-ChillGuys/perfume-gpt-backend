using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Requests.Products.ScentNotes;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class UpdateProductRequest
	{
		public string Name { get; set; } = null!;
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public string? Description { get; set; }
		public Gender Gender { get; set; }
		public string Origin { get; set; } = null!;
		public int ReleaseYear { get; set; }

		public List<int> OlfactoryFamilyIds { get; set; } = [];
		public List<ScentNoteDto> ScentNotes { get; set; } = [];
		public List<ProductAttributeDto>? Attributes { get; set; }

		// Image management for updates
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
		public List<Guid>? MediaIdsToDelete { get; set; }
	}
}
