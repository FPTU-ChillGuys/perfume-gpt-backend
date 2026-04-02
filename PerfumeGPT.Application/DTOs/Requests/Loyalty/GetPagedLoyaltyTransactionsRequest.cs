using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Loyalty
{
	public record GetPagedLoyaltyTransactionsRequest : PagingAndSortingQuery
	{
		public Guid? UserId { get; init; }
		public LoyaltyTransactionType? TransactionType { get; init; }
	}
}
