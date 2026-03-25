using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public class GetPagedUserLoyaltyTransactionsRequest : PagingAndSortingQuery
	{
		public LoyaltyTransactionType? TransactionType { get; set; }
	}
}
