using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ImportTicketsController : BaseApiController
	{
		private readonly IImportTicketService _importTicketService;
		private readonly IValidator<CreateImportTicketRequest> _createImportTicketValidator;
		private readonly IValidator<VerifyImportTicketRequest> _verifyImportTicketValidator;
		private readonly IValidator<UpdateImportStatusRequest> _updateImportStatusValidator;
		private readonly IValidator<UpdateImportRequest> _updateImportValidator;
		private readonly IValidator<UploadImportTicketFromExcelRequest> _createImportTicketFromExcelValidator;

		public ImportTicketsController(
			IImportTicketService importTicketService,
			IValidator<CreateImportTicketRequest> createImportTicketValidator,
			IValidator<VerifyImportTicketRequest> verifyImportTicketValidator,
			IValidator<UpdateImportStatusRequest> updateImportStatusValidator,
			IValidator<UpdateImportRequest> updateImportValidator,
			IValidator<UploadImportTicketFromExcelRequest> createImportTicketFromExcelValidator)
		{
			_importTicketService = importTicketService;
			_createImportTicketValidator = createImportTicketValidator;
			_verifyImportTicketValidator = verifyImportTicketValidator;
			_updateImportStatusValidator = updateImportStatusValidator;
			_updateImportValidator = updateImportValidator;
			_createImportTicketFromExcelValidator = createImportTicketFromExcelValidator;
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateImportTicket([FromBody] CreateImportTicketRequest request)
		{
			var validation = await ValidateRequestAsync(_createImportTicketValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _importTicketService.CreateImportTicketAsync(request, userId);
			return HandleResponse(response);
		}

		[HttpPost("excel-parser")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<CreateImportTicketRequest>>> UploadImportTicketFromExcel([FromForm] UploadImportTicketFromExcelRequest request)
		{
			var validation = await ValidateRequestAsync(_createImportTicketFromExcelValidator, request);
			if (validation != null) return validation;

			var response = await _importTicketService.UploadImportTicketFromExcelAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("excel-template")]
		[AllowAnonymous]
		[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ExcelTemplateResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> DownloadImportTemplate()
		{
			var response = await _importTicketService.GenerateImportTemplateAsync();

			if (response.Success && response.Payload != null)
			{
				return File(response.Payload.FileContent, response.Payload.ContentType, response.Payload.FileName);
			}

			return HandleResponse(response);
		}

		[HttpPost("{ticketId:guid}/verify")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> VerifyImportTicket([FromRoute] Guid ticketId, [FromBody] VerifyImportTicketRequest request)
		{
			var validation = await ValidateRequestAsync(_verifyImportTicketValidator, request);
			if (validation != null) return validation;

			var verifiedByUserId = GetCurrentUserId();
			var response = await _importTicketService.VerifyImportTicketAsync(ticketId, request, verifiedByUserId);
			return HandleResponse(response);
		}

		[HttpGet("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ImportTicketResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ImportTicketResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ImportTicketResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ImportTicketResponse>>> GetImportTicketById(Guid id)
		{
			var response = await _importTicketService.GetImportTicketByIdAsync(id);
			return HandleResponse(response);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ImportTicketListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ImportTicketListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ImportTicketListItem>>>> GetPagedImportTickets([FromQuery] GetPagedImportTicketsRequest request)
		{
			var response = await _importTicketService.GetImportTicketsAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}/status")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateImportStatus([FromRoute] Guid id, [FromBody] UpdateImportStatusRequest request)
		{
			var validation = await ValidateRequestAsync(_updateImportStatusValidator, request);
			if (validation != null) return validation;

			var response = await _importTicketService.UpdateImportStatusAsync(id, request);
			return HandleResponse(response);
		}

		[HttpPut("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateImportTicket([FromRoute] Guid id, [FromBody] UpdateImportRequest request)
		{
			var validation = await ValidateRequestAsync(_updateImportValidator, request);
			if (validation != null) return validation;

			var response = await _importTicketService.UpdateImportTicketAsync(id, request);
			return HandleResponse(response);
		}

		[HttpDelete("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteImportTicket([FromRoute] Guid id)
		{
			var response = await _importTicketService.DeleteImportTicketAsync(id);
			return HandleResponse(response);
		}
	}
}
