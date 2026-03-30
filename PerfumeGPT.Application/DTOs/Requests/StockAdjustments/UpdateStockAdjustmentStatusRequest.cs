using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public class UpdateStockAdjustmentStatusRequest
	{
		public StockAdjustmentStatus Status { get; set; }
		public string? Note { get; set; }
	}
}
