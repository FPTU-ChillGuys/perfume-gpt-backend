using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentsController : BaseApiController
	{
		private readonly IPaymentService _paymentService;
		private readonly IConfiguration _configuration;
		private readonly IValidator<ConfirmPaymentRequest> _confirmPaymentValidator;
		private readonly ILogger<PaymentsController> _logger;

		public PaymentsController(
			IPaymentService paymentService,
			IConfiguration configuration,
			ILogger<PaymentsController> logger,
			IValidator<ConfirmPaymentRequest> confirmPaymentValidator)
		{
			_paymentService = paymentService;
			_configuration = configuration;
			_logger = logger;
			_confirmPaymentValidator = confirmPaymentValidator;
		}

		[HttpGet("momo-return")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandleMomoCallback()
		{
			try
			{
				string frontendUrl = _configuration["Front-end:webUrl"] ?? "http://localhost:3000";

				if (!Request.Query.ContainsKey("resultCode") ||
					!Request.Query.ContainsKey("orderId"))
				{
					_logger.LogWarning("MoMo callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Invalid payment callback")}");
				}

				var result = await _paymentService.ProcessMomoReturnAsync(Request.Query);

				var resultCode = Request.Query["resultCode"].ToString();
				bool isSuccess = result.IsSuccess && resultCode == "0";
				var failureMessage = isSuccess ? null : "MoMo payment failed.";

				var redirectUrl = BuildMomoRedirectUrl(
					frontendUrl,
					isSuccess ? "success" : "failure",
					Request.Query,
					result.OrderId,
					result.PaymentId,
					failureMessage);

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing MoMo callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? "http://localhost:3000";
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
				string frontendUrl = _configuration["Front-end:webUrl"] ?? "http://localhost:3000";

				// Validate required parameters exist
				if (!Request.Query.ContainsKey("vnp_ResponseCode") ||
					!Request.Query.ContainsKey("vnp_TxnRef"))
				{
					_logger.LogWarning("VNPay callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Invalid payment callback")}");
				}

				// Process VNPay callback
				var result = await _paymentService.ProcessVnPayReturnAsync(Request.Query);

				// Determine success or failure
				var responseCode = Request.Query["vnp_ResponseCode"].ToString();
				bool isSuccess = result.IsSuccess && responseCode == "00";
				var failureMessage = isSuccess ? null : "VNPay payment failed.";

				// Build redirect URL
				var redirectUrl = BuildVnPayRedirectUrl(
					frontendUrl,
					isSuccess ? "success" : "failure",
					Request.Query,
					result.OrderId,
					result.PaymentId,
					failureMessage);

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing VNPay callback");
				string frontendUrl = _configuration["Front-end:webUrl"] ?? "http://localhost:3000";
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpPost("{paymentId:guid}/retry")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> RetryPayment([FromRoute] Guid paymentId, [FromBody] PaymentInformation? newMethod = null)
		{
			var response = await _paymentService.RetryPaymentWithMethodAsync(paymentId, newMethod);
			return HandleResponse(response);
		}

		[HttpPut("{paymentId:guid}/method")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> ChangePaymentMethod([FromRoute] Guid paymentId, [FromBody] PaymentInformation newMethod)
		{
			var response = await _paymentService.ChangePaymentMethodAsync(paymentId, newMethod);
			return HandleResponse(response);
		}

		[HttpPut("{paymentId:guid}/confirm")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> ConfirmPayment([FromRoute] Guid paymentId, [FromBody] ConfirmPaymentRequest request)
		{
			var validation = await ValidateRequestAsync(_confirmPaymentValidator, request);
			if (validation != null) return validation;

			var response = await _paymentService.UpdatePaymentStatusAsync(paymentId, request);
			return HandleResponse(response);
		}

		[HttpGet("management-transactions")]
		[Authorize(Roles = "staff,admin")]
		[ProducesResponseType(typeof(BaseResponse<PaymentTransactionOverviewResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PaymentTransactionOverviewResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PaymentTransactionOverviewResponse>>> GetTransactionsForManagement([FromQuery] GetPaymentTransactionsFilterRequest request)
		{
			var response = await _paymentService.GetTransactionsForManagementAsync(request);
			return HandleResponse(response);
		}

		private static string BuildVnPayRedirectUrl(string baseUrl, string status, IQueryCollection vnpayQuery, Guid? orderId = null, Guid? paymentId = null, string? errorMessage = null)
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

			// Add error message for failure
			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
			return $"{baseUrl}/payment/{status}{queryString}";
		}

		private static string BuildMomoRedirectUrl(string baseUrl, string status, IQueryCollection momoQuery, Guid? orderId = null, Guid? paymentId = null, string? errorMessage = null)
		{
			var paramsToForward = new[]
			{
				"orderId",
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
				queryParams.Add($"orderIdInternal={orderId.Value}");
			}

			if (paymentId.HasValue && paymentId.Value != Guid.Empty)
			{
				queryParams.Add($"paymentId={paymentId.Value}");
			}

			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
			return $"{baseUrl}/payment/{status}{queryString}";
		}
	}
}

