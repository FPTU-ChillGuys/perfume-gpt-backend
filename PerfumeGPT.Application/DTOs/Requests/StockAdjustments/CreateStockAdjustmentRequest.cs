using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public record CreateStockAdjustmentRequest
	{
		public DateTime AdjustmentDate { get; init; }
		public StockAdjustmentReason Reason { get; init; }
		public string? Note { get; init; }
		public required List<CreateStockAdjustmentDetailRequest> AdjustmentDetails { get; init; }
	}

	public record CreateStockAdjustmentDetailRequest
	{
		public Guid VariantId { get; init; }
		public Guid BatchId { get; init; }
		public int AdjustmentQuantity { get; init; }
		public string? Note { get; init; }
	}
}
