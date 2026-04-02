using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.StockAdjustments
{
	public record StockAdjustmentListItem
	{
		public Guid Id { get; init; }
		public required string CreatedByName { get; init; }
		public DateTime AdjustmentDate { get; init; }
		public StockAdjustmentReason Reason { get; init; }
		public StockAdjustmentStatus Status { get; init; }
		public int TotalItems { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
