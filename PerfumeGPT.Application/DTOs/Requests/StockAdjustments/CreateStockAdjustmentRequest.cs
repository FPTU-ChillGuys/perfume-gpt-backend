using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public class CreateStockAdjustmentRequest
	{
		public DateTime AdjustmentDate { get; set; }
		public StockAdjustmentReason Reason { get; set; }
		public string? Note { get; set; }
		public List<CreateStockAdjustmentDetailRequest> AdjustmentDetails { get; set; } = [];
	}

	public class CreateStockAdjustmentDetailRequest
	{
		public Guid VariantId { get; set; }
		public Guid BatchId { get; set; }
		public int AdjustmentQuantity { get; set; }
		public string? Note { get; set; }
	}
}
