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

		/// <summary>
		/// Create a new import ticket (without batches)
		/// </summary>
		[HttpPost]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> CreateImportTicket([FromBody] CreateImportTicketRequest request)
		{
			var validation = ValidateRequestBody<CreateImportTicketRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _importTicketService.CreateImportTicketAsync(request, userId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Verify import ticket by adding batches and updating stock
		/// </summary>
		[HttpPost("verify/{ticketId:guid}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> VerifyImportTicket([FromRoute] Guid ticketId, [FromBody] VerifyImportTicketRequest request)
		{
			var validation = ValidateRequestBody<VerifyImportTicketRequest>(request);
			if (validation != null) return validation;

			var verifiedByUserId = GetCurrentUserId();
			var response = await _importTicketService.VerifyImportTicketAsync(ticketId, request, verifiedByUserId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get import ticket by ID
		/// </summary>
		[HttpGet("{id}")]
		public async Task<ActionResult<BaseResponse<ImportTicketResponse>>> GetImportTicketById(Guid id)
		{
			var response = await _importTicketService.GetImportTicketByIdAsync(id);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get paged list of import tickets
		/// </summary>
		[HttpGet]
		public async Task<ActionResult<BaseResponse<PagedResult<ImportTicketListItem>>>> GetPagedImportTickets([FromQuery] GetPagedImportTicketsRequest request)
		{
			var response = await _importTicketService.GetPagedImportTicketsAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Update import ticket status
		/// </summary>
		[HttpPut("status/{id:guid}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> UpdateImportStatus([FromRoute] Guid id, [FromBody] UpdateImportTicketRequest request)
		{
			var validation = ValidateRequestBody<UpdateImportTicketRequest>(request);
			if (validation != null) return validation;

			var response = await _importTicketService.UpdateImportStatusAsync(id, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Delete import ticket
		/// </summary>
		[HttpDelete("{id}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteImportTicket(Guid id)
		{
			var response = await _importTicketService.DeleteImportTicketAsync(id);
			return HandleResponse(response);
		}
	}
}
