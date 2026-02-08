using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class GetPagedOrdersRequest : PagingAndSortingQuery
	{
		public OrderStatus? Status { get; set; }
		public OrderType? Type { get; set; }
		public PaymentStatus? PaymentStatus { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string? SearchTerm { get; set; }
	}
}
