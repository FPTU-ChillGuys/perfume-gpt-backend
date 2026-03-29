using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class LoyaltyTransactionService : ILoyaltyTransactionService
	{
		private readonly IUnitOfWork _unitOfWork;

		public LoyaltyTransactionService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>> GetLoyaltyHistoryAsync(Guid userId, GetPagedUserLoyaltyTransactionsRequest request)
		{
			if (userId == Guid.Empty)
				throw AppException.BadRequest("User ID is required.");

			var (items, totalCount) = await _unitOfWork.LoyaltyTransactions.GetPagedHistoryByUserAsync(userId, request);

			var pagedResult = new PagedResult<LoyaltyTransactionHistoryItemResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>> GetPagedLoyaltyTransactionsAsync(GetPagedLoyaltyTransactionsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.LoyaltyTransactions.GetPagedHistoryAsync(request);

			var pagedResult = new PagedResult<LoyaltyTransactionHistoryItemResponse>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount);

			return BaseResponse<PagedResult<LoyaltyTransactionHistoryItemResponse>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<LoyaltyTransactionTotalsResponse>> GetLoyaltyTotalsAsync(Guid userId)
		{
			if (userId == Guid.Empty)
				throw AppException.BadRequest("User ID is required.");

			var totals = await _unitOfWork.LoyaltyTransactions.GetTotalsAsync(userId);
			return BaseResponse<LoyaltyTransactionTotalsResponse>.Ok(totals);
		}

		public async Task<bool> PlusPointAsync(Guid userId, int points, Guid? orderId, bool saveChanges = true, string? reason = null)
		{
			if (userId == Guid.Empty)
				throw AppException.BadRequest("User ID is required.");

			if (points <= 0)
				throw AppException.BadRequest("Points must be greater than 0.");

			var transaction = LoyaltyTransaction.CreateEarn(userId, points, orderId, reason);

			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			if (saveChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved)
					throw AppException.Internal("Failed to add loyalty points.");
			}

			return true;
		}

		public async Task<bool> RedeemPointAsync(Guid userId, int points, Guid? voucherId, Guid? orderId, bool saveChanges = true, string? reason = null)
		{
			if (userId == Guid.Empty)
				throw AppException.BadRequest("User ID is required.");

			if (points <= 0)
				throw AppException.BadRequest("Points must be greater than 0.");

			var currentBalance = await _unitOfWork.LoyaltyTransactions.GetPointBalanceAsync(userId);
			if (currentBalance < points)
				throw AppException.BadRequest("Insufficient loyalty points.");

			var transaction = LoyaltyTransaction.CreateSpend(userId, points, voucherId, orderId, reason);

			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			if (saveChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved)
					throw AppException.Internal("Failed to redeem loyalty points.");
			}

			return true;
		}

		public async Task<BaseResponse<string>> ManualChangeAsync(Guid userId, ManualChangeRequest request)
		{
			if (request.TransactionType == Domain.Enums.LoyaltyTransactionType.Spend)
			{
				var currentBalance = await _unitOfWork.LoyaltyTransactions.GetPointBalanceAsync(userId);
				if (currentBalance < request.Points)
					throw AppException.BadRequest("Insufficient loyalty points.");
			}

			var transaction = LoyaltyTransaction.CreateManual(userId, request.TransactionType, request.Points, request.Reason);
			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to apply manual loyalty point change.");

			return BaseResponse<string>.Ok(transaction.Id.ToString(), "Manual loyalty point change applied successfully.");
		}
	}
}
