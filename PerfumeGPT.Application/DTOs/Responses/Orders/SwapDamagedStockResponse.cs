namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class SwapDamagedStockResponse
	{
		public Guid NewReservationId { get; set; }
		public Guid NewBatchId { get; set; }
		public string NewBatchCode { get; set; } = null!;
		public string? NewLocation { get; set; }
		public int ReservedQuantity { get; set; }
		public DateTime ExpiryDate { get; set; }
		public string Message { get; set; } = null!;
	}
}
