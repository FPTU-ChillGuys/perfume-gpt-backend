using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.StockAdjustments
{
	public class GetPagedStockAdjustmentsRequest : PagingAndSortingQuery
	{
		public StockAdjustmentReason? Reason { get; set; }
		public StockAdjustmentStatus? Status { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
	}
}
