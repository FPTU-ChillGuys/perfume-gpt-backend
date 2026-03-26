using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ILoyaltyTransactionRepository : IGenericRepository<LoyaltyTransaction>
	{
		Task<int> GetPointBalanceAsync(Guid userId);
		Task<(List<LoyaltyTransactionHistoryItemResponse> Items, int TotalCount)> GetPagedHistoryByUserAsync(Guid userId, GetPagedUserLoyaltyTransactionsRequest request);
		Task<(List<LoyaltyTransactionHistoryItemResponse> Items, int TotalCount)> GetPagedHistoryAsync(GetPagedLoyaltyTransactionsRequest request);
		Task<LoyaltyTransactionTotalsResponse> GetTotalsAsync(Guid userId);
	}
}
