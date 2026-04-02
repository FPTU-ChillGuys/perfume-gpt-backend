using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public record GetPagedUserLoyaltyTransactionsRequest : PagingAndSortingQuery
	{
		public LoyaltyTransactionType? TransactionType { get; init; }
	}
}
