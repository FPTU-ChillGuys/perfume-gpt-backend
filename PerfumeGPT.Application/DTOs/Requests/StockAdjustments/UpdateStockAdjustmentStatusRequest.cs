using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public record UpdateStockAdjustmentStatusRequest
	{
		public StockAdjustmentStatus Status { get; init; }
		public string? Note { get; init; }
	}
}
