namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	public class BatchResponse
	{
		public Guid Id { get; set; }
		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int ImportQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
