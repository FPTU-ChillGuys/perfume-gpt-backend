using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.StorePolicies;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.StorePolicies;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class StorePoliciesController : BaseApiController
	{
		private readonly IStorePolicyService _storePolicyService;

		public StorePoliciesController(IStorePolicyService storePolicyService)
		{
			_storePolicyService = storePolicyService;
		}

		[HttpGet("current")]
		[ProducesResponseType(typeof(BaseResponse<StorePolicyResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<StorePolicyResponse>>> GetCurrentPolicyAsync()
		{
			var response = await _storePolicyService.GetCurrentPolicyAsync();
			return HandleResponse(response);
		}

		[HttpPut("current")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<StorePolicyResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<StorePolicyResponse>>> UpdateCurrentPolicyAsync([FromBody] UpdateStorePolicyRequest request)
		{
			var response = await _storePolicyService.UpdateCurrentPolicyAsync(request);
			return HandleResponse(response);
		}
	}
}
