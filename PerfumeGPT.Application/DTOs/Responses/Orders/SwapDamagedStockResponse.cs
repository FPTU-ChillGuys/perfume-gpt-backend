namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record SwapDamagedStockResponse
	{
		public Guid NewReservationId { get; init; }
		public Guid NewBatchId { get; init; }
		public required string NewBatchCode { get; init; }
		public string? NewLocation { get; init; }
		public int ReservedQuantity { get; init; }
		public DateTime ExpiryDate { get; init; }
		public required string Message { get; init; }
	}
}
