using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public class GetPagedLoyaltyTransactionsRequest : PagingAndSortingQuery
	{
		public Guid? UserId { get; set; }
		public LoyaltyTransactionType? TransactionType { get; set; }
	}
}
