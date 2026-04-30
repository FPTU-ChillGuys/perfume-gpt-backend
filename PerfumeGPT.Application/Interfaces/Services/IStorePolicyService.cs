using PerfumeGPT.Application.DTOs.Requests.StorePolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StorePolicies;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IStorePolicyService
	{
		Task<BaseResponse<StorePolicyResponse>> GetCurrentPolicyAsync();
		Task<BaseResponse<StorePolicyResponse>> UpdateCurrentPolicyAsync(UpdateStorePolicyRequest request);
	}
}
