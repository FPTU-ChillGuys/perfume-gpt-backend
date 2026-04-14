using PerfumeGPT.Application.DTOs.Requests.SystemPolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Policies;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ISystemPolicyService
	{
		Task<BaseResponse<SystemPolicyResponse>> GetByPolicyCodeAsync(string policyCode);
		Task<BaseResponse<SystemPolicyResponse>> UpdatePolicyAsync(string policyCode, SystemPolicyUpdateRequest request);
	}
}
