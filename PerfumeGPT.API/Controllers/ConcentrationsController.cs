using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ConcentrationsController : BaseApiController
	{
		public IConcentrationService _concentrationService;

		public ConcentrationsController(IConcentrationService concentrationService)
		{
			_concentrationService = concentrationService;
		}

		[HttpGet("lookup")]
		public async Task<ActionResult<BaseResponse<List<ConcentrationLookupDto>>>> GetConcentrationLookup()
		{
			var response = await _concentrationService.GetConcentrationLookup();
			return Ok(response);
		}
	}
}
