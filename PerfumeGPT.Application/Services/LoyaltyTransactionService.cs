using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using static PerfumeGPT.Domain.Entities.LoyaltyTransaction;

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
			var totals = await _unitOfWork.LoyaltyTransactions.GetTotalsAsync(userId);
			return BaseResponse<LoyaltyTransactionTotalsResponse>.Ok(totals);
		}

		public async Task<bool> PlusPointAsync(Guid userId, int points, Guid? orderId, bool saveChanges = true, string? reason = null)
		{
			var info = new EarnTransactionInfo
			{
				Points = points,
				OrderId = orderId,
				Reason = reason
			};

			var transaction = LoyaltyTransaction.CreateEarn(userId, info);

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
			var currentBalance = await _unitOfWork.LoyaltyTransactions.GetPointBalanceAsync(userId);
			if (currentBalance < points)
				throw AppException.BadRequest("Insufficient loyalty points.");

			var info = new SpendTransactionInfo
			{
				Points = points,
				VoucherId = voucherId,
				OrderId = orderId,
				Reason = reason
			};

			var transaction = LoyaltyTransaction.CreateSpend(userId, info);

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

			var info = new ManualTransactionInfo
			{
				TransactionType = request.TransactionType,
				Points = request.Points,
				Reason = request.Reason
			};

			var transaction = LoyaltyTransaction.CreateManual(userId, info);
			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Failed to apply manual loyalty point change.");

			return BaseResponse<string>.Ok(transaction.Id.ToString(), "Manual loyalty point change applied successfully.");
		}
	}
}
