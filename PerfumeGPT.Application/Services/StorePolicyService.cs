using PerfumeGPT.Application.DTOs.Requests.StorePolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StorePolicies;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class StorePolicyService : IStorePolicyService
	{
		private readonly IUnitOfWork _unitOfWork;

		public StorePolicyService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task<BaseResponse<StorePolicyResponse>> GetCurrentPolicyAsync()
		{
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync()
				?? throw AppException.NotFound("Không tìm thấy cấu hình chính sách cửa hàng.");

			return BaseResponse<StorePolicyResponse>.Ok(MapPolicy(policy), "Lấy cấu hình chính sách cửa hàng thành công.");
		}

		public async Task<BaseResponse<StorePolicyResponse>> UpdateCurrentPolicyAsync(UpdateStorePolicyRequest request)
		{
			var policy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync()
				?? throw AppException.NotFound("Không tìm thấy cấu hình chính sách cửa hàng.");

			policy.UpdateDepositPolicy(
				request.RequiredDepositPercentage,
				request.DepositTimeoutMinutes,
				request.IsDepositRequiredForCOD);
			policy.UpdateReviewPolicy(request.ReviewRewardPoints);
			policy.UpdateStockAdjustmentPolicy(request.StockAdjustmentAutoApprovalThreshold);

			_unitOfWork.StorePolicies.Update(policy);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<StorePolicyResponse>.Ok(MapPolicy(policy), "Cập nhật cấu hình chính sách cửa hàng thành công.");
		}

		private static StorePolicyResponse MapPolicy(Domain.Entities.StorePolicy policy)
		{
			return new StorePolicyResponse
			{
				Id = policy.Id,
				RequiredDepositPercentage = policy.RequiredDepositPercentage,
				DepositTimeoutMinutes = policy.DepositTimeoutMinutes,
				IsDepositRequiredForCOD = policy.IsDepositRequiredForCOD,
				ReviewRewardPoints = policy.ReviewRewardPoints,
				StockAdjustmentAutoApprovalThreshold = policy.StockAdjustmentAutoApprovalThreshold
			};
		}
	}
}
