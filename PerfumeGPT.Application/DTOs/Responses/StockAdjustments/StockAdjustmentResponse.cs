using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.StockAdjustments
{
	public class StockAdjustmentResponse
	{
		public Guid Id { get; set; }
		public Guid CreatedById { get; set; }
		public string CreatedByName { get; set; } = string.Empty;
		public Guid? VerifiedById { get; set; }
		public string? VerifiedByName { get; set; }
		public DateTime AdjustmentDate { get; set; }
		public StockAdjustmentReason Reason { get; set; }
		public string? Note { get; set; }
		public StockAdjustmentStatus Status { get; set; }
		public List<StockAdjustmentDetailResponse> AdjustmentDetails { get; set; } = [];
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
	}

	public class StockAdjustmentDetailResponse
	{
		public Guid Id { get; set; }
		public Guid ProductVariantId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		public string VariantSku { get; set; } = string.Empty;
		public Guid BatchId { get; set; }
		public string BatchCode { get; set; } = string.Empty;
		public int AdjustmentQuantity { get; set; }
		public int ApprovedQuantity { get; set; }
		public string? Note { get; set; }
	}
}
