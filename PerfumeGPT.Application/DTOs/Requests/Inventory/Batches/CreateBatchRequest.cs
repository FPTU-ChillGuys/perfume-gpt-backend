namespace PerfumeGPT.Application.DTOs.Requests.Inventory.Batches
{
	public class CreateBatchRequest
	{
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int Quantity { get; set; }
	}
}
