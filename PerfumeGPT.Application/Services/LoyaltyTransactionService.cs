using MapsterMapper;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class LoyaltyTransactionService : ILoyaltyTransactionService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public LoyaltyTransactionService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<GetLoyaltyPointResponse>> GetLoyaltyPointsAsync(Guid userId)
		{
			var points = await _unitOfWork.LoyaltyTransactions.GetPointBalanceAsync(userId);
			var response = new GetLoyaltyPointResponse
			{
				UserId = userId,
				PointBalance = points
			};
			return BaseResponse<GetLoyaltyPointResponse>.Ok(response);
		}

		public async Task<bool> PlusPointAsync(Guid userId, int points, Guid? orderId, bool saveChanges = true)
		{
			if (userId == Guid.Empty) return false;
			if (points <= 0) return false;

			var transaction = new LoyaltyTransaction
			{
				UserId = userId,
				OrderId = orderId,
				TransactionType = LoyaltyTransactionType.Earn,
				PointsChanged = points,
				Reason = orderId.HasValue ? $"Earned from Order {orderId}" : "Manual point addition"
			};

			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			if (saveChanges)
				return await _unitOfWork.SaveChangesAsync();

			return true;
		}

		public async Task<bool> RedeemPointAsync(Guid userId, int points, Guid? voucherId, Guid? orderId, bool saveChanges = true)
		{
			if (userId == Guid.Empty) return false;
			if (points <= 0) return false;

			var currentBalance = await _unitOfWork.LoyaltyTransactions.GetPointBalanceAsync(userId);
			if (currentBalance < points)
				return false;

			var transaction = new LoyaltyTransaction
			{
				UserId = userId,
				VoucherId = voucherId,
				TransactionType = LoyaltyTransactionType.Spend,
				PointsChanged = -points,
				Reason = voucherId.HasValue ? $"Redeemed for Voucher {voucherId}" : orderId.HasValue ? $"Redeemed for Order {orderId} returned" : "Manual point redemption"
			};

			await _unitOfWork.LoyaltyTransactions.AddAsync(transaction);

			if (saveChanges)
				return await _unitOfWork.SaveChangesAsync();

			return true;
		}
	}
}
