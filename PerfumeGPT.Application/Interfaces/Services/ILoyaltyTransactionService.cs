using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ILoyaltyTransactionService
	{
		Task<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>> GetLoyaltyHistoryAsync(Guid userId, GetPagedUserLoyaltyTransactionsRequest request);
		Task<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>> GetPagedLoyaltyTransactionsAsync(GetPagedLoyaltyTransactionsRequest request);
		Task<BaseResponse<LoyaltyTransactionTotalsResponse>> GetLoyaltyTotalsAsync(Guid userId);
		Task<bool> PlusPointAsync(Guid userId, int points, Guid? orderId, bool saveChanges = true, string? reason = null);
		Task<bool> RedeemPointAsync(Guid userId, int points, Guid? voucherId, Guid? orderId, bool saveChanges = true, string? reason = null);
		Task<BaseResponse<string>> ManualChangeAsync(Guid userId, ManualChangeRequest request);
	}
}
