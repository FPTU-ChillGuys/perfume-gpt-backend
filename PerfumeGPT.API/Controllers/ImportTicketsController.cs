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

		public ImportTicketsController(IImportTicketService importTicketService)
		{
			_importTicketService = importTicketService;
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateImportTicket([FromBody] CreateImportTicketRequest request)
		{
			var validation = ValidateRequestBody<CreateImportTicketRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _importTicketService.CreateImportTicketAsync(request, userId);
			return HandleResponse(response);
		}

		[HttpPost("upload-excel")]
		[Authorize]
       [ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<CreateImportTicketRequest>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<CreateImportTicketRequest>>> CreateImportTicketFromExcel([FromForm] CreateImportTicketFromExcelRequest request)
		{
			var validation = ValidateRequestBody<CreateImportTicketFromExcelRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _importTicketService.CreateImportTicketFromExcelAsync(request, userId);
			return HandleResponse(response);
		}

		[HttpGet("download-template")]
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

			return HandleResponse(BaseResponse<ExcelTemplateResponse>.Fail(response.Message, response.ErrorType));
		}

		[HttpPost("{ticketId:guid}/verify")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> VerifyImportTicket([FromRoute] Guid ticketId, [FromBody] VerifyImportTicketRequest request)
		{
			var validation = ValidateRequestBody<VerifyImportTicketRequest>(request);
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
			var validation = ValidateRequestBody<UpdateImportStatusRequest>(request);
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
			var validation = ValidateRequestBody<UpdateImportRequest>(request);
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
		public async Task<ActionResult<BaseResponse<bool>>> DeleteImportTicket(Guid id)
		{
			var response = await _importTicketService.DeleteImportTicketAsync(id);
			return HandleResponse(response);
		}
	}
}
