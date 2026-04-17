using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.SystemPolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Policies;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/policies")]
	[ApiController]
	public class PoliciesController : BaseApiController
	{
		private readonly ISystemPolicyService _systemPolicyService;

		public PoliciesController(ISystemPolicyService systemPolicyService)
		{
			_systemPolicyService = systemPolicyService;
		}

		[HttpGet("{policyCode}")]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<SystemPolicyResponse>>> GetPolicyByCodeAsync([FromRoute] string policyCode)
		{
			if (string.IsNullOrWhiteSpace(policyCode))
             throw AppException.BadRequest("Bắt buộc cung cấp mã chính sách.");

			var policy = await _systemPolicyService.GetByPolicyCodeAsync(policyCode);
			return HandleResponse(policy);
		}

		[HttpPut("{policyCode}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<SystemPolicyResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<SystemPolicyResponse>>> UpdatePolicyAsync([FromRoute] string policyCode, [FromBody] SystemPolicyUpdateRequest request)
		{
			if (string.IsNullOrWhiteSpace(policyCode))
             throw AppException.BadRequest("Bắt buộc cung cấp mã chính sách.");
			var updatedPolicy = await _systemPolicyService.UpdatePolicyAsync(policyCode, request);
			return HandleResponse(updatedPolicy);
		}
	}
}
