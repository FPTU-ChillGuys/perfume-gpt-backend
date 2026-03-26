using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class VouchersController : BaseApiController
	{
		private readonly IVoucherService _voucherService;

		public VouchersController(IVoucherService voucherService)
		{
			_voucherService = voucherService;
		}

		#region Admin Endpoints

		[HttpPost]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateVoucher([FromBody] CreateVoucherRequest request)
		{
			var validation = ValidateRequestBody<CreateVoucherRequest>(request);
			if (validation != null) return validation;

			var response = await _voucherService.CreateRegularVoucherAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{voucherId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateVoucher(Guid voucherId, [FromBody] UpdateVoucherRequest request)
		{
			var validation = ValidateRequestBody<UpdateVoucherRequest>(request);
			if (validation != null) return validation;

			var response = await _voucherService.UpdateVoucherAsync(voucherId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{voucherId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteVoucher(Guid voucherId)
		{
			var response = await _voucherService.DeleteVoucherAsync(voucherId);
			return HandleResponse(response);
		}

		[HttpGet("{voucherId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetVoucher(Guid voucherId)
		{
			var response = await _voucherService.GetVoucherByIdAsync(voucherId);
			return HandleResponse(response);
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VoucherResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<VoucherResponse>>>> GetVouchers(
			[FromQuery] GetPagedVouchersRequest request)
		{
			var response = await _voucherService.GetPagedVouchersAsync(request);
			return HandleResponse(response);
		}

		#endregion

		#region User Endpoints

		[HttpGet("redeemable-list")]
		public async Task<ActionResult<BaseResponse<List<RedeemableVoucherResponse>>>> GetRedeemableVouchers([FromQuery] GetPagedRedeemableVouchersRequest request)
		{
			var response = await _voucherService.GetRedeemableVouchersAsync(request);
			return HandleResponse(response);
		}

		[HttpPost("redeem")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> RedeemVoucher([FromBody] RedeemVoucherRequest request)
		{
			var validation = ValidateRequestBody<RedeemVoucherRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _voucherService.RedeemVoucherAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("available")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<AvailableVoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<AvailableVoucherResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<AvailableVoucherResponse>>>> GetAvailableVouchers([FromQuery] GetPagedAvailableVouchersRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.GetAvailableVouchersAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<UserVoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<UserVoucherResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<UserVoucherResponse>>>> GetMyVouchers(
			[FromQuery] GetPagedUserVouchersRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.GetUserVouchersAsync(userId, request);
			return HandleResponse(response);
		}

		#endregion
	}
}
