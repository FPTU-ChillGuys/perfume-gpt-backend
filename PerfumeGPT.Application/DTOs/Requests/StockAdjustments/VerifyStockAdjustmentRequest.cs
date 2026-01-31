namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public class VerifyStockAdjustmentRequest
	{
		public List<VerifyStockAdjustmentDetailRequest> AdjustmentDetails { get; set; } = [];
	}

	public class VerifyStockAdjustmentDetailRequest
	{
		public Guid DetailId { get; set; }
		public int ApprovedQuantity { get; set; }
		public string? Note { get; set; }
	}
}
