using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public record GetPagedStockAdjustmentsRequest : PagingAndSortingQuery
	{
		public StockAdjustmentReason? Reason { get; init; }
		public StockAdjustmentStatus? Status { get; init; }
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
	}
}
