using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PaymentsController : BaseApiController
	{
		private readonly IPaymentService _paymentService;
		private readonly IConfiguration _configuration;
		private readonly ILogger<PaymentsController> _logger;

		public PaymentsController(IPaymentService paymentService, IConfiguration configuration, ILogger<PaymentsController> logger)
		{
			_paymentService = paymentService;
			_configuration = configuration;
			_logger = logger;
		}

		[HttpGet("vnpay-return")]
		[ProducesResponseType(StatusCodes.Status302Found)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> HandleVnPayCallback()
		{
			try
			{
				// Get frontend URL
				string frontendUrl = _configuration["Front-end:webUrlHttps"] ?? "https://localhost:3000";

				// Validate required parameters exist
				if (!Request.Query.ContainsKey("vnp_ResponseCode") ||
					!Request.Query.ContainsKey("vnp_TxnRef"))
				{
					_logger.LogWarning("VNPay callback missing required parameters");
					return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Invalid payment callback")}");
				}

				// Process VNPay callback
				await _paymentService.ProcessVnPayReturnAsync(Request.Query);
				var result = await _paymentService.GetVnPayReturnResponseAsync(Request.Query);

				// Determine success or failure
				var responseCode = Request.Query["vnp_ResponseCode"].ToString();
				bool isSuccess = result?.Success == true && responseCode == "00";

				// Build redirect URL
				var redirectUrl = BuildVnPayRedirectUrl(
					frontendUrl,
					isSuccess ? "success" : "failure",
					Request.Query,
					result?.Payload?.OrderId,
					isSuccess ? null : result?.Message);

				return Redirect(redirectUrl);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing VNPay callback");
				string frontendUrl = _configuration["Front-end:webUrlHttps"] ?? "https://localhost:3000";
				return Redirect($"{frontendUrl}/payment/failure?error={Uri.EscapeDataString("Payment processing error")}");
			}
		}

		[HttpPost("retry/{paymentId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> RetryPayment(Guid paymentId, [FromBody] PaymentInformation? newMethod = null)
		{
			var response = await _paymentService.RetryPaymentWithMethodAsync(paymentId, newMethod);
			return HandleResponse(response);
		}

		[HttpPut("change-method/{paymentId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> ChangePaymentMethod(Guid paymentId, [FromBody] PaymentInformation newMethod)
		{
			var response = await _paymentService.ChangePaymentMethodAsync(paymentId, newMethod);
			return HandleResponse(response);
		}

		[HttpPut("confirm/{paymentId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> ConfirmPayment(Guid paymentId, [FromQuery] bool isSuccess = true, [FromQuery] string? failureReason = null)
		{
			var response = await _paymentService.UpdatePaymentStatusAsync(paymentId, isSuccess, failureReason);
			return HandleResponse(response);
		}

		private static string BuildVnPayRedirectUrl(
			string baseUrl,
			string status,
			IQueryCollection vnpayQuery,
			Guid? orderId = null,
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

			// Add error message for failure
			if (!string.IsNullOrWhiteSpace(errorMessage))
			{
				queryParams.Add($"error={Uri.EscapeDataString(errorMessage)}");
			}

			var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
			return $"{baseUrl}/payment/{status}{queryString}";
		}
	}
}

