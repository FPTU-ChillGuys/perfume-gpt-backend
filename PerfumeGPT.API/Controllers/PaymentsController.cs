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

		public PaymentsController(IPaymentService paymentService, IConfiguration configuration)
		{
			_paymentService = paymentService;
			_configuration = configuration;
		}

		/// <summary>
		/// Handle VNPay payment callback
		/// </summary>
		[HttpGet("vnpay-return")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> HandleVnPayCallback()
		{
			// Update payment status based on VNPay return query parameters
			await _paymentService.ProcessVnPayReturnAsync(Request.Query);

			// Call service to let backend process the VNPay return (update DB, statuses, etc.)
			var result = await _paymentService.GetVnPayReturnResponseAsync(Request.Query);
			return HandleResponse(result);

			//// Get frontend base URL from configuration (fallback to localhost if missing)
			//string frontendUrl = _configuration["Front-end:webUrl"] ?? "http://localhost:3000";

			//// Keys to forward from VNPay query to front-end. Exclude secure hash for safety.
			//var forwardKeys = new[]
			//{
			//	"vnp_Amount",
			//	"vnp_BankCode",
			//	"vnp_BankTranNo",
			//	"vnp_CardType",
			//	"vnp_OrderInfo",
			//	"vnp_PayDate",
			//	"vnp_ResponseCode",
			//	"vnp_TmnCode",
			//	"vnp_TransactionNo",
			//	"vnp_TransactionStatus",
			//	"vnp_TxnRef"
			//};

			//var qsParts = new List<string>();

			//// Helper to read and add query param if present
			//void TryAddQuery(string key)
			//{
			//	if (Request.Query.TryGetValue(key, out var val) && !StringValues.IsNullOrEmpty(val))
			//	{
			//		qsParts.Add($"{key}={Uri.EscapeDataString(val.ToString())}");
			//	}
			//}

			//foreach (var k in forwardKeys)
			//{
			//	TryAddQuery(k);
			//}

			//// Include subscriptionId from service payload when available
			//if (result != null && result.Payload != null && result.Payload.OrderId != Guid.Empty)
			//{
			//	qsParts.Add($"orderId={result.Payload.OrderId}");
			//}

			//// Also add a normalized amount (divide VNPay value by 100 if numeric) for front-end convenience
			//if (Request.Query.TryGetValue("vnp_Amount", out var vnpAmt) && !StringValues.IsNullOrEmpty(vnpAmt))
			//{
			//	if (long.TryParse(vnpAmt.ToString(), out var raw))
			//	{
			//		var display = (raw / 100m).ToString("0.##");
			//		qsParts.Add($"amount={Uri.EscapeDataString(display)}");
			//	}
			//	else
			//	{
			//		qsParts.Add($"amount={Uri.EscapeDataString(vnpAmt.ToString())}");
			//	}
			//}

			//string redirectUrl;
			//if (result!.Success)
			//{
			//	// Redirect to frontend success page with important params so client can display details
			//	var queryString = qsParts.Count > 0 ? "?" + string.Join("&", qsParts) : string.Empty;
			//	redirectUrl = $"{frontendUrl}/payment/success{queryString}";
			//}
			//else
			//{
			//	// On failure include error message and forwarded params
			//	qsParts.Add($"error={Uri.EscapeDataString(result.Message ?? "Unknown error")}");
			//	var queryString = "?" + string.Join("&", qsParts);
			//	redirectUrl = $"{frontendUrl}/payment/failed{queryString}";
			//}

			//return Redirect(redirectUrl);
		}

		/// <summary>
		/// Retry a failed payment with optional new payment method
		/// </summary>
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

		/// <summary>
		/// Change payment method for a pending payment
		/// </summary>
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

		/// <summary>
		/// Confirm payment status (success or failure)
		/// </summary>
		[HttpPut("confirm/{paymentId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<bool>>> ConfirmPayment(Guid paymentId, [FromQuery] bool isSuccess = true, [FromQuery] string? failureReason = null)
		{
			var response = await _paymentService.UpdatePaymentStatusAsync(paymentId, isSuccess, failureReason);
			return HandleResponse(response);
		}
	}
}

