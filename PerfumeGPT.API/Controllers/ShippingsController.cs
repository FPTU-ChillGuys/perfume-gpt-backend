using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.Shippings;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Shippings;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ShippingsController : BaseApiController
	{
		private readonly IShippingService _shippingService;
		private readonly IGHNService _ghnService;
		private readonly ILogger<ShippingsController> _logger;
		private readonly IConfiguration _configuration;
		private readonly IValidator<GetOrderInfoRequest> _getOrderInfoRequestValidator;
		private readonly IValidator<GhnOrderStatusWebhookRequest> _ghnOrderStatusWebhookRequestValidator;

		public ShippingsController(
			IShippingService shippingService,
			IGHNService ghnService,
			IValidator<GetOrderInfoRequest> getOrderInfoRequestValidator,
			IValidator<GhnOrderStatusWebhookRequest> ghnOrderStatusWebhookRequestValidator,
			ILogger<ShippingsController> logger,
			IConfiguration configuration)
		{
			_shippingService = shippingService;
			_ghnService = ghnService;
			_getOrderInfoRequestValidator = getOrderInfoRequestValidator;
			_ghnOrderStatusWebhookRequestValidator = ghnOrderStatusWebhookRequestValidator;
			_logger = logger;
			_configuration = configuration;
		}

		[HttpGet("user/{userId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ShippingInfoListItem>>>> GetPagedShippingsByUserId([FromRoute] Guid userId, [FromQuery] GetPagedShippingsRequest request)
		{
			var response = await _shippingService.GetPagedShippingInfosByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("me")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ShippingInfoListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ShippingInfoListItem>>>> GetPagedShippingsForCurrentUser([FromQuery] GetPagedShippingsRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _shippingService.GetPagedShippingInfosByUserIdAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpPost("user/{userId:guid}/sync-shipping-status")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> SyncShippingStatusByUserId([FromRoute] Guid userId)
		{
			var response = await _shippingService.SyncShippingStatusByUserIdAsync(userId);
			return HandleResponse(response);
		}

		// Inactive webhook processing, Waiting contact GHN to enable webhook.
		[AllowAnonymous]
		[HttpPost("ghn/webhook-order-status")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SyncShippingStatusFromGhnWebhook([FromBody] GhnOrderStatusWebhookRequest request)
		{
			var validation = await ValidateRequestAsync(_ghnOrderStatusWebhookRequestValidator, request);
			if (validation != null) return validation;

			var response = await _shippingService.SyncShippingStatusByWebhookAsync(request.OrderCode, request.Status);
			return HandleResponse(response);
		}

		// Inactive webhook processing, Waiting contact GHN to enable webhook.
		[AllowAnonymous]
		// SỬA ĐỔI 1: Yêu cầu thêm tham số token bí mật vào URL
		// URL config trên GHN sẽ có dạng: /api/ghn/webhook-order-status/YOUR-SECRET-UUID-HERE
		[HttpPost("ghn/webhook-order-status/{token}")]
		// SỬA ĐỔI 2: Xóa bỏ các ProducesResponseType lỗi, Webhook luôn trả về 200
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		public async Task<ActionResult> SyncShippingStatusFromGhnWebhook(
			[FromRoute] string token,
			[FromBody] GhnOrderStatusWebhookRequest request)
		{
			// 1. Kiểm tra Token bảo mật (Lấy từ appsettings.json)
			var expectedToken = _configuration["GHN:WebhookSecret"];
			if (token != expectedToken)
			{
				_logger.LogWarning("Phát hiện truy cập giả mạo Webhook GHN!");
				// Vẫn trả 200 để Hacker không biết là bị chặn (hoặc trả 403 cũng được vì hacker gọi chứ không phải GHN)
				return StatusCode(403);
			}

			// 2. Bỏ qua các sự kiện không phải cập nhật trạng thái
			if (!string.IsNullOrEmpty(request.Type) && request.Type != "switch_status" && request.Type != "create")
			{
				return Ok(BaseResponse<string>.Ok("Ignored non-status event"));
			}

			try
			{
				// Bạn có thể giữ validation nếu muốn, nhưng nếu lỗi thì vẫn trả 200
				var validationResult = await _ghnOrderStatusWebhookRequestValidator.ValidateAsync(request);
				if (!validationResult.IsValid)
				{
					_logger.LogWarning("GHN Webhook Payload không hợp lệ: {Errors}", validationResult.Errors);
					return Ok(); // Ép GHN ngừng gửi
				}

				await _shippingService.SyncShippingStatusByWebhookAsync(request.OrderCode, request.Status);
			}
			catch (Exception ex)
			{
				// Ghi log lại lỗi để tự xử lý, nhưng tuyệt đối không trả về 500 cho GHN
				_logger.LogError(ex, "Lỗi đồng bộ trạng thái đơn hàng GHN {OrderCode}", request.OrderCode);
			}

			// Luôn luôn trả về 200 OK để GHN biết mình đã nhận được tin nhắn
			return Ok(BaseResponse<string>.Ok("Processed successfully"));
		}

		[HttpPost("me/sync-shipping-status")]
		public async Task<ActionResult<BaseResponse<string>>> SyncShippingStatusForCurrentUser()
		{
			var userId = GetCurrentUserId();

			var response = await _shippingService.SyncShippingStatusByUserIdAsync(userId);
			return HandleResponse(response);
		}

		[HttpPost("order-info-url")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> GetOrderInfoUrlAsync([FromBody] GetOrderInfoRequest request)
		{
			var validation = await ValidateRequestAsync(_getOrderInfoRequestValidator, request);
			if (validation != null) return validation;

			var response = await _ghnService.GetOrderInfoUrlAsync(request);
			return HandleResponse(response);
		}
	}
}
