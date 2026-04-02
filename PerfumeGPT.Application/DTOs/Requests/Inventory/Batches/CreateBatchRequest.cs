namespace PerfumeGPT.Application.DTOs.Requests.Inventory.Batches
{
	public record CreateBatchRequest
	{
		public required string BatchCode { get; init; }
		public DateTime ManufactureDate { get; init; }
		public DateTime ExpiryDate { get; init; }
		public int Quantity { get; init; }
	}
}
