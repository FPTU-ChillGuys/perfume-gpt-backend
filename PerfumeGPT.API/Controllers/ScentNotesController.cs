using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.ScentNotes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.ScentNotes;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ScentNotesController : BaseApiController
	{
		private readonly IScentNoteService _scentNoteService;
		private readonly IValidator<CreateScentNoteRequest> _createValidator;
		private readonly IValidator<UpdateScentNoteRequest> _updateValidator;

		public ScentNotesController(IScentNoteService scentNoteService, IValidator<CreateScentNoteRequest> createValidator, IValidator<UpdateScentNoteRequest> updateValidator)
		{
			_scentNoteService = scentNoteService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<ScentNoteLookupResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ScentNoteLookupResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<ScentNoteLookupResponse>>>> GetScentNoteLookupList()
		{
			var result = await _scentNoteService.GetScentNoteLookupListAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<ScentNoteResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<ScentNoteResponse>>>> GetAllScentNotesAsync()
		{
			var result = await _scentNoteService.GetAllScentNotesAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ScentNoteResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ScentNoteResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<ScentNoteResponse>>> GetScentNoteByIdAsync(int id)
		{
			var result = await _scentNoteService.GetScentNoteByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<ScentNoteResponse>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<ScentNoteResponse>>> CreateScentNoteAsync([FromBody] CreateScentNoteRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var result = await _scentNoteService.CreateScentNoteAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<ScentNoteResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ScentNoteResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<ScentNoteResponse>>> UpdateScentNoteAsync(int id, [FromBody] UpdateScentNoteRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var result = await _scentNoteService.UpdateScentNoteAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteScentNoteAsync(int id)
		{
			var result = await _scentNoteService.DeleteScentNoteAsync(id);
			return HandleResponse(result);
		}
	}
}
