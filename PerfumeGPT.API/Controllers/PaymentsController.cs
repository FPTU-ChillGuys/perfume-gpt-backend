using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Hubs;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentsController : BaseApiController
	{
		private readonly IPaymentService _paymentService;
		private readonly IConfiguration _configuration;
		private readonly ILogger<PaymentsController> _logger;
		private readonly IHubContext<PosHub, IPosClient> _posHubContext;

		public PaymentsController(
			IPaymentService paymentService,
			IConfiguration configuration,
			ILogger<PaymentsController> logger,
			IHubContext<PosHub, IPosClient> posHubContext)
		{
			_paymentService = paymentService;
			_configuration = configuration;
			_logger = logger;
			_posHubContext = posHubContext;
		}

		[HttpGet("momo-return")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandleMomoCallback()
		{
			try
			{
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");

				if (!Request.Query.ContainsKey("resultCode") ||
					!Request.Query.ContainsKey("orderId"))
				{
					_logger.LogWarning("MoMo callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Thanh toán MoMo không hợp lệ")}");
				}

				var result = await _paymentService.ProcessMomoReturnAsync(Request.Query);

				var resultCode = Request.Query["resultCode"].ToString();
				bool isSuccess = result.IsSuccess && resultCode == "0";
				var failureMessage = isSuccess ? null : "Thanh toán MoMo thất bại.";

				var redirectUrl = BuildMomoRedirectUrl(
					frontendUrl,
					isSuccess ? "success" : "failure",
					Request.Query,
					result.OrderId,
					result.PaymentId,
					result.OrderCode,
					result.PosSessionId,
					failureMessage);

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing MoMo callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpGet("payos-return")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandlePayOsReturnCallback()
		{
			try
			{
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");

				if (!Request.Query.ContainsKey("orderCode") && !Request.Query.ContainsKey("paymentId"))
				{
					_logger.LogWarning("PayOS callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Thanh toán PayOS không hợp lệ")}");
				}

				var result = await _paymentService.ProcessPayOsReturnAsync(Request.Query);
				var status = result.IsSuccess ? "success" : "failure";
				var failureMessage = result.IsSuccess ? null : "PayOS thanh toán thất bại.";

				var redirectUrl = BuildPayOsRedirectUrl(
					frontendUrl,
					status,
					Request.Query,
					result.OrderId,
					result.PaymentId,
					result.OrderCode,
					result.PosSessionId,
					failureMessage);
				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing PayOS callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpGet("payos-cancel")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandlePayOsCancelCallback()
		{
			try
			{
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");
				var result = await _paymentService.ProcessPayOsReturnAsync(Request.Query, isCancelCallback: true);

				var redirectUrl = BuildPayOsRedirectUrl(
					frontendUrl,
					"failure",
					Request.Query,
					result.OrderId,
					result.PaymentId,
					result.OrderCode,
					result.PosSessionId,
					"PayOS payment was cancelled.");

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing PayOS cancel callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpGet("vnpay-return")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandleVnPayCallback()
		{
			try
			{
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");

				// Validate required parameters exist
				if (!Request.Query.ContainsKey("vnp_ResponseCode") ||
					!Request.Query.ContainsKey("vnp_TxnRef"))
				{
					_logger.LogWarning("VNPay callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Thanh toán VNPay không hợp lệ")}");
				}

				// Process VNPay callback
				var result = await _paymentService.ProcessVnPayReturnAsync(Request.Query);

				// Determine success or failure
				var responseCode = Request.Query["vnp_ResponseCode"].ToString();
				bool isSuccess = result.IsSuccess && responseCode == "00";
				var failureMessage = isSuccess ? null : "VNPay thanh toán thất bại.";

				// Build redirect URL
				var redirectUrl = BuildVnPayRedirectUrl(
					frontendUrl,
					isSuccess ? "success" : "failure",
					Request.Query,
					result.OrderId,
					result.PaymentId,
					result.OrderCode,
					result.PosSessionId,
					failureMessage);

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing VNPay callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? throw new Exception("Thiếu web url trong cấu hình");
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpPost("{paymentId:guid}/retry")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> RetryPayment([FromRoute] Guid paymentId, [FromBody] RetryOrChangePaymentRequest newMethod)
		{
			var response = await _paymentService.RetryOrChangePaymentMethodAsync(paymentId, newMethod);
			return HandleResponse(response);
		}

		[HttpPut("{paymentId:guid}/confirm")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> ConfirmPayment([FromRoute] Guid paymentId, [FromBody] ConfirmPaymentRequest request)
		{
			var response = await _paymentService.UpdatePaymentStatusAsync(paymentId, request);

			if (response.Success && request.IsSuccess && !string.IsNullOrWhiteSpace(request.PosSessionId))
			{
				await _posHubContext.Clients.Group(request.PosSessionId)
					.PaymentCompleted(new PosPaymentCompletedDto
					{
						OrderId = Guid.Empty,
						PaymentId = paymentId,
						Status = "Success",
						Message = "Thanh toán thành công"
					});
			}

			return HandleResponse(response);
		}

		[HttpGet("management-transactions")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PaymentTransactionOverviewResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PaymentTransactionOverviewResponse>>> GetTransactionsForManagement([FromQuery] GetPaymentTransactionsFilterRequest request)
		{
			var response = await _paymentService.GetTransactionsForManagementAsync(request);
			return HandleResponse(response);
		}

		private static string BuildVnPayRedirectUrl(
			  string baseUrl,
			  string status,
			  IQueryCollection vnpayQuery,
			  Guid? orderId = null,
			  Guid? paymentId = null,
			  string? orderCode = null,
			  string? posSessionId = null,
			  string? errorMessage = null)
		{
			// Parameters to forward to frontend
			var paramsToForward = new[]
			{
				"vnp_TxnRef",
				"vnp_Amount",
				"vnp_BankCode",
				"vnp_CardType",
				"vnp_PayDate",
				"vnp_TransactionNo",
				"vnp_OrderInfo",
				"vnp_ResponseCode"
			};

			var queryParams = new List<string>();

			// Add VNPay parameters
			foreach (var paramName in paramsToForward)
			{
				if (vnpayQuery.TryGetValue(paramName, out var value) && !string.IsNullOrWhiteSpace(value))
				{
					queryParams.Add($"{paramName}={Uri.EscapeDataString(value.ToString())}");
				}
			}

			// Add orderId if available
			if (orderId.HasValue && orderId.Value != Guid.Empty)
			{
				queryParams.Add($"orderId={orderId.Value}");
			}

			if (paymentId.HasValue && paymentId.Value != Guid.Empty)
			{
				queryParams.Add($"paymentId={paymentId.Value}");
			}

			if (!string.IsNullOrWhiteSpace(orderCode))
			{
				queryParams.Add($"orderCode={Uri.EscapeDataString(orderCode)}");
			}

			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				queryParams.Add($"sessionId={Uri.EscapeDataString(posSessionId)}");
			}

			// Add error message for failure
			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
			return $"{baseUrl}/payment/{status}{queryString}";
		}

		private static string BuildMomoRedirectUrl(
			string baseUrl,
			string status,
			IQueryCollection momoQuery,
			Guid? orderId = null,
			Guid? paymentId = null,
			string? orderCode = null,
			string? posSessionId = null,
			string? errorMessage = null)
		{
			var paramsToForward = new[]
			{
				"requestId",
				"amount",
				"transId",
				"orderInfo",
				"resultCode",
				"message"
			};

			var queryParams = new List<string>();

			foreach (var paramName in paramsToForward)
			{
				if (momoQuery.TryGetValue(paramName, out var value) && !string.IsNullOrWhiteSpace(value))
				{
					queryParams.Add($"{paramName}={Uri.EscapeDataString(value.ToString())}");
				}
			}

			if (orderId.HasValue && orderId.Value != Guid.Empty)
			{
				queryParams.Add($"orderId={orderId.Value}");
			}

			if (paymentId.HasValue && paymentId.Value != Guid.Empty)
			{
				queryParams.Add($"paymentId={paymentId.Value}");
			}

			if (!string.IsNullOrWhiteSpace(orderCode))
			{
				queryParams.Add($"orderCode={Uri.EscapeDataString(orderCode)}");
			}

			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				queryParams.Add($"sessionId={Uri.EscapeDataString(posSessionId)}");
			}

			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
			return $"{baseUrl}/payment/{status}{queryString}";
		}

		private static string BuildPayOsRedirectUrl(
			string baseUrl,
			string status,
			IQueryCollection payOsQuery,
			Guid? orderId = null,
			Guid? paymentId = null,
			string? orderCode = null,
			string? posSessionId = null,
			string? errorMessage = null)
		{
			var paramsToForward = new[]
			{
				"code",
				"id",
				"cancel",
				"status"
			};

			var queryParams = new List<string>();

			foreach (var paramName in paramsToForward)
			{
				if (payOsQuery.TryGetValue(paramName, out var value) && !string.IsNullOrWhiteSpace(value))
				{
					queryParams.Add($"{paramName}={Uri.EscapeDataString(value.ToString())}");
				}
			}

			if (orderId.HasValue && orderId.Value != Guid.Empty)
			{
				queryParams.Add($"orderId={orderId.Value}");
			}

			if (paymentId.HasValue && paymentId.Value != Guid.Empty)
			{
				queryParams.Add($"paymentId={paymentId.Value}");
			}

			if (!string.IsNullOrWhiteSpace(orderCode))
			{
				queryParams.Add($"orderCode={Uri.EscapeDataString(orderCode)}");
			}

			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				queryParams.Add($"sessionId={Uri.EscapeDataString(posSessionId)}");
			}

			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
			return $"{baseUrl}/payment/{status}{queryString}";
		}
	}
}

