using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductInforResponse
	{
		public string ProductCode { get; set; } = string.Empty;
		public string BrandName { get; set; } = string.Empty;
		public string Origin { get; set; } = string.Empty;
		public int ReleaseYear { get; set; }
		public Gender Gender { get; set; }
		public string ScentGroup { get; set; } = string.Empty;
		public string Style { get; set; } = string.Empty;
		public string TopNotes { get; set; } = string.Empty;
		public string HeartNotes { get; set; } = string.Empty;
		public string BaseNotes { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
	}
}
