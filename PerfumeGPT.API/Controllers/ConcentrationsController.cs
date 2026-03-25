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
		public async Task<ActionResult<BaseResponse<List<ConcentrationLookupDto>>>> GetConcentrationLookup()
		{
			var response = await _concentrationService.GetConcentrationLookup();
			return Ok(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<ConcentrationResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<ConcentrationResponse>>>> GetAllConcentrationsAsync()
		{
			var result = await _concentrationService.GetAllConcentrationsAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> GetConcentrationByIdAsync(int id)
		{
			var result = await _concentrationService.GetConcentrationByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> CreateConcentrationAsync([FromBody] CreateConcentrationRequest request)
		{
			var validation = ValidateRequestBody<CreateConcentrationRequest>(request);
			if (validation != null) return validation;

			var result = await _concentrationService.CreateConcentrationAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ConcentrationResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<ConcentrationResponse>>> UpdateConcentrationAsync(int id, [FromBody] UpdateConcentrationRequest request)
		{
			var validation = ValidateRequestBody<UpdateConcentrationRequest>(request);
			if (validation != null) return validation;

			var result = await _concentrationService.UpdateConcentrationAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteConcentrationAsync(int id)
		{
			var result = await _concentrationService.DeleteConcentrationAsync(id);
			return HandleResponse(result);
		}
	}
}
