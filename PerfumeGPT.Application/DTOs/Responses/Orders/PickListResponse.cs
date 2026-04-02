namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public record PickListResponse
	{
		public Guid OrderId { get; init; }
		public required string Code { get; init; }
		public required List<PickListItemResponse> Items { get; init; }
	}

	public record PickListItemResponse
	{
		public Guid OrderDetailId { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public int Quantity { get; init; }
		public required List<PickListBatchInfo> Batches { get; init; }
	}

	public record PickListBatchInfo
	{
		public Guid ReservationId { get; init; }
		public Guid BatchId { get; init; }
		public required string BatchCode { get; init; }
		public string? Note { get; init; }
		public int ReservedQuantity { get; init; }
		public DateTime ExpiryDate { get; init; }
	}
}
