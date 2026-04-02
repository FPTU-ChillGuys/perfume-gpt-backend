using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.StockAdjustments
{
	public record StockAdjustmentResponse
	{
		public Guid Id { get; init; }
		public Guid CreatedById { get; init; }
		public required string CreatedByName { get; init; }
		public Guid? VerifiedById { get; init; }
		public string? VerifiedByName { get; init; }
		public DateTime AdjustmentDate { get; init; }
		public StockAdjustmentReason Reason { get; init; }
		public string? Note { get; init; }
		public StockAdjustmentStatus Status { get; init; }
		public required List<StockAdjustmentDetailResponse> AdjustmentDetails { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime? UpdatedAt { get; init; }
	}

	public record StockAdjustmentDetailResponse
	{
		public Guid Id { get; init; }
		public Guid ProductVariantId { get; init; }
		public required string ProductName { get; init; }
		public required string VariantSku { get; init; }
		public Guid BatchId { get; init; }
		public required string BatchCode { get; init; }
		public int AdjustmentQuantity { get; init; }
		public int ApprovedQuantity { get; init; }
		public string? Note { get; init; }
	}
}
