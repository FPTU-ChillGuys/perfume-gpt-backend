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

		/// <summary>
		/// Create a new voucher (Admin only)
		/// </summary>
		[HttpPost]
		//[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateVoucher([FromBody] CreateVoucherRequest request)
		{
			var validation = ValidateRequestBody<CreateVoucherRequest>(request);
			if (validation != null) return validation;

			var response = await _voucherService.CreateVoucherAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Update an existing voucher (Admin only)
		/// </summary>
		[HttpPut("{voucherId}")]
		//[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status409Conflict)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateVoucher(
			Guid voucherId,
			[FromBody] UpdateVoucherRequest request)
		{
			var validation = ValidateRequestBody<UpdateVoucherRequest>(request);
			if (validation != null) return validation;

			var response = await _voucherService.UpdateVoucherAsync(voucherId, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Delete a voucher (Admin only)
		/// </summary>
		[HttpDelete("{voucherId}")]
		//[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteVoucher(Guid voucherId)
		{
			var response = await _voucherService.DeleteVoucherAsync(voucherId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get voucher by ID (Admin only)
		/// </summary>
		[HttpGet("{voucherId}")]
		//[Authorize(Roles = "Admin")]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetVoucher(Guid voucherId)
		{
			var response = await _voucherService.GetVoucherByIdAsync(voucherId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get paginated list of vouchers (Admin only)
		/// </summary>
		[HttpGet]
		//[Authorize(Roles = "Admin")]
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

		[HttpGet("my-vouchers")]
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

		[HttpPost("apply")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<ApplyVoucherResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ApplyVoucherResponse>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<ApplyVoucherResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ApplyVoucherResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ApplyVoucherResponse>>> ApplyVoucher(
			[FromBody] ApplyVoucherRequest request)
		{
			var validation = ValidateRequestBody<ApplyVoucherRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _voucherService.ApplyVoucherToOrderAsync(userId, request);
			return HandleResponse(response);
		}

		#endregion
	}
}
