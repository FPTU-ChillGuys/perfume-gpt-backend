using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.StockAdjustments
{
	public class StockAdjustmentListItem
	{
		public Guid Id { get; set; }
		public string CreatedByName { get; set; } = string.Empty;
		public DateTime AdjustmentDate { get; set; }
		public StockAdjustmentReason Reason { get; set; }
		public StockAdjustmentStatus Status { get; set; }
		public int TotalItems { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
