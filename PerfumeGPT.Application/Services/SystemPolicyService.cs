using PerfumeGPT.Application.DTOs.Requests.SystemPolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Policies;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class SystemPolicyService : ISystemPolicyService
	{
		private readonly IUnitOfWork _unitOfWork;

		public SystemPolicyService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

		public async Task<BaseResponse<SystemPolicyResponse>> GetByPolicyCodeAsync(string policyCode)
		{
			var policy = await _unitOfWork.SystemPolicyRepository.GetResponseByPolicyCodeAsync(policyCode) ??
				throw AppException.NotFound($"System policy with code '{policyCode}' not found.");

			return BaseResponse<SystemPolicyResponse>.Ok(policy);
		}

		public async Task<BaseResponse<SystemPolicyResponse>> UpdatePolicyAsync(string policyCode, SystemPolicyUpdateRequest request)
		{
			var policy = await _unitOfWork.SystemPolicyRepository.GetByPolicyCodeAsync(policyCode) ??
				throw AppException.NotFound($"System policy with code '{policyCode}' not found.");

			policy.Update(request.Title, request.HtmlContent);
			_unitOfWork.SystemPolicyRepository.Update(policy);
			await _unitOfWork.SaveChangesAsync();
			return BaseResponse<SystemPolicyResponse>.Ok(new SystemPolicyResponse
			{
				PolicyCode = policy.Id,
				Title = policy.Title,
				HtmlContent = policy.HtmlContent,
				LastUpdated = policy.UpdatedAt ?? policy.CreatedAt
			}, "System policy updated successfully.");
		}
	}
}
