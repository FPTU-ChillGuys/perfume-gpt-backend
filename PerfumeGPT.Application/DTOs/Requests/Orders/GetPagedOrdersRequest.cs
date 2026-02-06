using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Orders
{
	public class GetPagedOrdersRequest : PagingAndSortingQuery
	{
		/// <summary>
		/// Filter by order status.
		/// </summary>
		public OrderStatus? Status { get; set; }

		/// <summary>
		/// Filter by order type (Online/Offline).
		/// </summary>
		public OrderType? Type { get; set; }

		/// <summary>
		/// Filter by payment status.
		/// </summary>
		public PaymentStatus? PaymentStatus { get; set; }

		/// <summary>
		/// Filter orders created from this date.
		/// </summary>
		public DateTime? FromDate { get; set; }

		/// <summary>
		/// Filter orders created until this date.
		/// </summary>
		public DateTime? ToDate { get; set; }

		/// <summary>
		/// Search by order ID or customer name.
		/// </summary>
		public string? SearchTerm { get; set; }
	}
}
