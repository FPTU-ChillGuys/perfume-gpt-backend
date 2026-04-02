namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public record VerifyStockAdjustmentRequest
	{
		public required List<VerifyStockAdjustmentDetailRequest> AdjustmentDetails { get; init; }
	}

	public record VerifyStockAdjustmentDetailRequest
	{
		public Guid DetailId { get; init; }
		public int ApprovedQuantity { get; init; }
		public string? Note { get; init; }
	}
}
