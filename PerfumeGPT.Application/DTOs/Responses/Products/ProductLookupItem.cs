using PerfumeGPT.Application.DTOs.Responses.Media;

namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductLookupItem
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = null!;
		public string BrandName { get; set; } = null!;
		public MediaResponse? PrimaryImage { get; set; }
	}
}
