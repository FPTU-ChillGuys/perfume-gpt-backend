using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
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
	}
}
