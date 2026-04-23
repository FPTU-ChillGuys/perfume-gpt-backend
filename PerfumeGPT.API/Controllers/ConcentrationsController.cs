using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations;
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
		[ProducesResponseType(typeof(BaseResponse<List<ConcentrationLookupDto>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ConcentrationLookupDto>>>> GetConcentrationLookup()
		{
			var response = await _concentrationService.GetConcentrationLookup();
			return Ok(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<ConcentrationResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ConcentrationResponse>>>> GetAllConcentrationsAsync()
		{
			var result = await _concentrationService.GetAllConcentrationsAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> GetConcentrationByIdAsync([FromRoute] int id)
		{
			var validationResult = ValidatePositiveInt(id, "Concentration ID");
			if (validationResult != null) return validationResult;

			var result = await _concentrationService.GetConcentrationByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> CreateConcentrationAsync([FromBody] CreateConcentrationRequest request)
		{
			var result = await _concentrationService.CreateConcentrationAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> UpdateConcentrationAsync([FromRoute] int id, [FromBody] UpdateConcentrationRequest request)
		{
			var validationResult = ValidatePositiveInt(id, "Concentration ID");
			if (validationResult != null) return validationResult;

			var result = await _concentrationService.UpdateConcentrationAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteConcentrationAsync([FromRoute] int id)
		{
			var validationResult = ValidatePositiveInt(id, "Concentration ID");
			if (validationResult != null) return validationResult;

			var result = await _concentrationService.DeleteConcentrationAsync(id);
			return HandleResponse(result);
		}
	}
}
