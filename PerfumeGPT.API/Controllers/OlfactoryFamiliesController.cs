using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
using PerfumeGPT.Application.DTOs.Requests.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class OlfactoryFamiliesController : BaseApiController
	{
		private readonly IOlfactoryFamilyService _olfactoryFamilyService;

		public OlfactoryFamiliesController(IOlfactoryFamilyService olfactoryFamilyService)
		{
			_olfactoryFamilyService = olfactoryFamilyService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<OlfactoryLookupResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<OlfactoryLookupResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<OlfactoryLookupResponse>>>> GetOlfactoryFamilyLookupList()
		{
			var result = await _olfactoryFamilyService.GetOlfactoryFamilyLookupListAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<OlfactoryFamilyResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<OlfactoryFamilyResponse>>>> GetAllOlfactoryFamiliesAsync()
		{
			var result = await _olfactoryFamilyService.GetAllOlfactoryFamiliesAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<OlfactoryFamilyResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<OlfactoryFamilyResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<OlfactoryFamilyResponse>>> GetOlfactoryFamilyByIdAsync(int id)
		{
			var result = await _olfactoryFamilyService.GetOlfactoryFamilyByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<OlfactoryFamilyResponse>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<OlfactoryFamilyResponse>>> CreateOlfactoryFamilyAsync([FromBody] CreateOlfactoryFamilyRequest request)
		{
			var result = await _olfactoryFamilyService.CreateOlfactoryFamilyAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<OlfactoryFamilyResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<OlfactoryFamilyResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<OlfactoryFamilyResponse>>> UpdateOlfactoryFamilyAsync(int id, [FromBody] UpdateOlfactoryFamilyRequest request)
		{
			var result = await _olfactoryFamilyService.UpdateOlfactoryFamilyAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteOlfactoryFamilyAsync(int id)
		{
			var result = await _olfactoryFamilyService.DeleteOlfactoryFamilyAsync(id);
			return HandleResponse(result);
		}
	}
}
