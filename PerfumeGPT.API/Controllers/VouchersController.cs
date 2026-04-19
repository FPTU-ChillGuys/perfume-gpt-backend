using FluentValidation;
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
		private readonly IValidator<CreateVoucherRequest> _createValidator;
		private readonly IValidator<UpdateVoucherRequest> _updateValidator;

		public VouchersController(
			IVoucherService voucherService,
			IValidator<CreateVoucherRequest> createValidator,
			IValidator<UpdateVoucherRequest> updateValidator)
		{
			_voucherService = voucherService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
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
			var validation = await ValidateRequestAsync(_createValidator, request);
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
		public async Task<ActionResult<BaseResponse<string>>> UpdateVoucher([FromRoute] Guid voucherId, [FromBody] UpdateVoucherRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var response = await _voucherService.UpdateVoucherAsync(voucherId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{voucherId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteVoucher([FromRoute] Guid voucherId)
		{
			var response = await _voucherService.DeleteVoucherAsync(voucherId);
			return HandleResponse(response);
		}

		[HttpGet("{voucherId}")]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<VoucherResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<VoucherResponse>>> GetVoucher([FromRoute] Guid voucherId)
		{
			var response = await _voucherService.GetVoucherByIdAsync(voucherId);
			return HandleResponse(response);
		}

		[HttpGet]
		[Authorize(Roles = "admin")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VoucherResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<VoucherResponse>>>> GetVouchers([FromQuery] GetPagedVouchersRequest request)
		{
			var response = await _voucherService.GetPagedVouchersAsync(request);
			return HandleResponse(response);
		}
		#endregion Admin Endpoints



		#region User Endpoints
		[HttpGet("redeemable")]
		public async Task<ActionResult<BaseResponse<List<RedeemableVoucherResponse>>>> GetRedeemableVouchersV2([FromQuery] GetPagedRedeemableVouchersRequest request)
		{
			var currentUserId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : Guid.Empty;
			Guid? userId = currentUserId == Guid.Empty ? null : currentUserId;
			var response = await _voucherService.GetRedeemableVouchersAsync(request, userId);
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

		[HttpGet("variant/{variantId}/applicable")]
		[ProducesResponseType(typeof(BaseResponse<List<ApplicableVoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ApplicableVoucherResponse>>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<List<ApplicableVoucherResponse>>>> GetApplicableVouchersForVariant([FromRoute] Guid variantId)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.GetProductVariantVouchersAsync(variantId, userId, null);
			return HandleResponse(response);
		}

		[HttpGet("/api/user-vouchers/me")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<UserVoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<UserVoucherResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<UserVoucherResponse>>>> GetMyUserVouchers([FromQuery] GetPagedUserVouchersRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _voucherService.GetUserVouchersAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpPost("applicable")]
		[ProducesResponseType(typeof(BaseResponse<List<ApplicableVoucherResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ApplicableVoucherResponse>>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<List<ApplicableVoucherResponse>>>> GetApplicableVouchers([FromBody] GetApplicableVouchersRequest request)
		{
			var validation = ValidateRequestBody<GetApplicableVouchersRequest>(request);
			if (validation != null) return validation;

			var currentUserId = User.Identity?.IsAuthenticated == true ? GetCurrentUserId() : Guid.Empty;
			var resolvedCustomerId = request.CustomerId ?? (currentUserId == Guid.Empty ? null : currentUserId);
			var effectiveRequest = request with { CustomerId = resolvedCustomerId };

			var response = await _voucherService.GetApplicableVouchersAsync(effectiveRequest);
			return HandleResponse(response);
		}
		#endregion User Endpoints
	}
}
