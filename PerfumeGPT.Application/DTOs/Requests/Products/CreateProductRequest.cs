using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class CreateProductRequest
	{
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public int FamilyId { get; set; }
		public Gender Gender { get; set; }
		public string? Description { get; set; }
		public string? TopNotes { get; set; }
		public string? MiddleNotes { get; set; }
		public string? BaseNotes { get; set; }
	}
}
