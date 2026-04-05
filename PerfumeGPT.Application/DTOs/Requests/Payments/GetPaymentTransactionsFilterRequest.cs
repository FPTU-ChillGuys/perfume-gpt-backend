using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Payments
{
   public record GetPaymentTransactionsFilterRequest : PagingAndSortingQuery
	{
		public DateTime? FromDate { get; init; }
		public DateTime? ToDate { get; init; }
		public PaymentMethod? PaymentMethod { get; init; }
		public TransactionType? TransactionType { get; init; }
	}
}
