namespace PerfumeGPT.Application.DTOs.Responses.Orders
{
	public class PickListResponse
	{
		public Guid OrderId { get; set; }
		public string OrderCode { get; set; } = null!;
		public List<PickListItemResponse> Items { get; set; } = [];
	}

	public class PickListItemResponse
	{
		public Guid OrderDetailId { get; set; }
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = null!;
		public int Quantity { get; set; }
		public List<PickListBatchInfo> Batches { get; set; } = [];
	}

	public class PickListBatchInfo
	{
		public Guid ReservationId { get; set; }
		public Guid BatchId { get; set; }
		public string BatchCode { get; set; } = null!;
		public string? Location { get; set; }
		public int ReservedQuantity { get; set; }
		public DateTime ExpiryDate { get; set; }
	}
}
