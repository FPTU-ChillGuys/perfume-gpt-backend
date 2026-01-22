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
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetVoucher(Guid voucherId)
		{
			var response = await _voucherService.GetVoucherAsync(voucherId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get paginated list of vouchers (Admin only)
		/// </summary>
		[HttpGet]
		//[Authorize(Roles = "Admin")]
		public async Task<ActionResult<BaseResponse<PagedResult<VoucherResponse>>>> GetVouchers(
			[FromQuery] GetPagedVouchersRequest request)
		{
			var response = await _voucherService.GetVouchersAsync(request);
			return HandleResponse(response);
		}

		#endregion

		#region User Endpoints

		/// <summary>
		/// Redeem a voucher using loyalty points
		/// </summary>
		[HttpPost("redeem")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> RedeemVoucher([FromBody] RedeemVoucherRequest request)
		{
			var validation = ValidateRequestBody<RedeemVoucherRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _voucherService.RedeemVoucherAsync(userId, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get user's redeemed vouchers
		/// </summary>
		[HttpGet("my-vouchers")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<PagedResult<UserVoucherResponse>>>> GetMyVouchers(
			[FromQuery] int pageNumber = 1,
			[FromQuery] int pageSize = 10)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.GetUserVouchersAsync(userId, pageNumber, pageSize);
			return HandleResponse(response);
		}

		/// <summary>
		/// Apply voucher to calculate order discount
		/// </summary>
		[HttpPost("apply")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<ApplyVoucherResponse>>> ApplyVoucher(
			[FromBody] ApplyVoucherRequest request)
		{
			var validation = ValidateRequestBody<ApplyVoucherRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var response = await _voucherService.ApplyVoucherToOrderAsync(userId, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Validate if a voucher can be applied
		/// </summary>
		[HttpGet("validate/{voucherCode}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<bool>>> ValidateVoucher(string voucherCode)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.ValidateToApplyVoucherAsync(voucherCode, userId);
			return HandleResponse(response);
		}

		#endregion
	}
}
