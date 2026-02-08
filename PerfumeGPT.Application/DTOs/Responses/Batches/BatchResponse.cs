namespace PerfumeGPT.Application.DTOs.Responses.Batches
{
	/// <summary>
	/// Base batch response with essential properties.
	/// Used for nested batch info in import tickets.
	/// </summary>
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
