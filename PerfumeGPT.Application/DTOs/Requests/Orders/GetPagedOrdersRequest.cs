using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public record GetPagedOrdersRequest : PagingAndSortingQuery
	{
		public OrderStatus? Status { get; init; }
		public OrderType? Type { get; init; }
		public PaymentStatus? PaymentStatus { get; init; }
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
		public string? SearchTerm { get; init; }
	}
}
