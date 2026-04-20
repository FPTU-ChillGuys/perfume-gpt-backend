using PerfumeGPT.Application.DTOs.Requests.Loyalty;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;
using static PerfumeGPT.Domain.Entities.LoyaltyTransaction;

namespace PerfumeGPT.Application.Services
{
	public class LoyaltyTransactionService : ILoyaltyTransactionService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly INotificationService _notificationService;

		public LoyaltyTransactionService(
			IUnitOfWork unitOfWork,
			IUserRepository userRepository,
			INotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_notificationService = notificationService;
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
			var user = await _userRepository.GetByIdAsync(userId)
				  ?? throw AppException.NotFound("Không tìm thấy người dùng.");

			var info = new EarnTransactionInfo
			{
				Points = points,
				OrderId = orderId,
				Reason = reason
			};

			user.EarnPoints(info);
			_userRepository.Update(user);

			if (saveChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved)
					throw AppException.Internal("Cộng điểm tích lũy thất bại.");

				await _notificationService.SendToUserAsync(
					userId,
					"Điểm tích lũy được cộng",
					$"Bạn vừa nhận được {points} điểm tích lũy. Tổng điểm hiện tại: {user.PointBalance}.",
					NotificationType.Success,
					orderId,
					orderId.HasValue ? NotifiReferecneType.Order : null);
			}

			return true;
		}

		public async Task<bool> RedeemPointAsync(Guid userId, int points, Guid? voucherId, Guid? orderId, bool saveChanges = true, string? reason = null)
		{
			var user = await _userRepository.GetByIdAsync(userId)
			  ?? throw AppException.NotFound("Không tìm thấy người dùng.");

			var info = new SpendTransactionInfo
			{
				Points = points,
				VoucherId = voucherId,
				OrderId = orderId,
				Reason = reason
			};

			user.SpendPoints(info);
			_userRepository.Update(user);

			if (saveChanges)
			{
				var saved = await _unitOfWork.SaveChangesAsync();
				if (!saved)
					throw AppException.Internal("Đổi điểm tích lũy thất bại.");
			}

			return true;
		}

		public async Task<BaseResponse<string>> ManualChangeAsync(Guid userId, ManualChangeRequest request)
		{
			var user = await _userRepository.GetByIdAsync(userId)
			  ?? throw AppException.NotFound("Không tìm thấy người dùng.");

			var info = new ManualTransactionInfo
			{
				TransactionType = request.TransactionType,
				Points = request.Points,
				Reason = request.Reason
			};

			var transaction = user.AdjustPointsManual(info);
			_userRepository.Update(user);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Áp dụng thay đổi điểm tích lũy thủ công thất bại.");

			if (transaction.TransactionType == LoyaltyTransactionType.Earn)
			{
				await _notificationService.SendToUserAsync(
					userId,
					"Điểm tích lũy được cộng",
					$"Bạn vừa nhận được {request.Points} điểm tích lũy từ điều chỉnh thủ công. Tổng điểm hiện tại: {user.PointBalance}.",
					NotificationType.Success);
			}

			return BaseResponse<string>.Ok(transaction.Id.ToString(), "Áp dụng thay đổi điểm tích lũy thủ công thành công.");
		}
	}
}
