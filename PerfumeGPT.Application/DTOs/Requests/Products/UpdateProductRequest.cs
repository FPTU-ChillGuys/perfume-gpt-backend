namespace PerfumeGPT.Application.DTOs.Requests.Products
{
	public class UpdateProductRequest : CreateProductRequest
	{
		// Image management for updates
		public List<Guid>? TemporaryMediaIdsToAdd { get; set; }
		public List<Guid>? MediaIdsToDelete { get; set; }
	}
}
