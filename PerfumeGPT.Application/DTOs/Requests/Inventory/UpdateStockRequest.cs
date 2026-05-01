namespace PerfumeGPT.Application.DTOs.Requests.Inventory
{
	public record UpdateStockRequest
	{
		public int LowStockThreshold { get; init; }
	}
}
