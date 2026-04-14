using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Ledgers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Ledgers;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LedgersController : BaseApiController
	{
		private readonly ILedgerService _ledgerService;

		public LedgersController(ILedgerService ledgerService)
		{
			_ledgerService = ledgerService;
		}

		[HttpGet("cash-flow")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<CashFlowLedgerItemResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<CashFlowLedgerItemResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<CashFlowLedgerItemResponse>>>> GetCashFlowLedgers([FromQuery] GetCashFlowLedgersRequest request)
		{
			var response = await _ledgerService.GetCashFlowLedgersAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("inventory")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<InventoryLedgerItemResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<InventoryLedgerItemResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<InventoryLedgerItemResponse>>>> GetInventoryLedgers([FromQuery] GetInventoryLedgersRequest request)
		{
			var response = await _ledgerService.GetInventoryLedgersAsync(request);
			return HandleResponse(response);
		}
	}
}
