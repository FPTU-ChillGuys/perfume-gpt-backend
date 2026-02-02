using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class UpdateProductRequest
	{
		// Product basic information
		public string? Name { get; set; }
		public int BrandId { get; set; }
		public int CategoryId { get; set; }
		public int FamilyId { get; set; }
		public Gender Gender { get; set; }
		public string? Description { get; set; }
		public string? TopNotes { get; set; }
		public string? MiddleNotes { get; set; }
		public string? BaseNotes { get; set; }

		// Image management for updates
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
		public List<Guid>? MediaIdsToDelete { get; set; }
	}
}
