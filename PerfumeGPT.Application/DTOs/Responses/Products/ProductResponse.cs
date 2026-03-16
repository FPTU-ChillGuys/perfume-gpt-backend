using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductScentNoteResponse
	{
		public int NoteId { get; set; }
		public string Name { get; set; } = null!;
		public NoteType Type { get; set; }
	}

	public class ProductOlfactoryFamilyResponse
	{
		public int OlfactoryFamilyId { get; set; }
		public string Name { get; set; } = null!;
	}

	public class ProductResponse
	{
		public Guid Id { get; set; }
		public string? Name { get; set; }
		public Gender Gender { get; set; }
		public string Origin { get; set; } = null!;
		public int ReleaseYear { get; set; }
		public int BrandId { get; set; }
		public string BrandName { get; set; } = null!;
		public int CategoryId { get; set; }
		public string CategoryName { get; set; } = null!;
		public string? Description { get; set; }
		public int NumberOfVariants { get; set; }
		public List<MediaResponse> Media { get; set; } = [];
		public List<ProductVariantResponse> Variants { get; set; } = [];
		public List<ProductAttributeResponse> Attributes { get; set; } = [];

		public List<ProductOlfactoryFamilyResponse> OlfactoryFamilies { get; set; } = [];
		public List<ProductScentNoteResponse> ScentNotes { get; set; } = [];
	}
}

