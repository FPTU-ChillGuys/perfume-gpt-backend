using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class MomoService : IMomoService
	{
		private readonly IConfiguration _configuration;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public MomoService(
			IConfiguration configuration,
			IHttpClientFactory httpClientFactory,
			IWebHostEnvironment webHostEnvironment)
		{
			_configuration = configuration;
			_httpClientFactory = httpClientFactory;
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<OrderCheckoutResponse> CreatePaymentUrlAsync(HttpContext context, MomoPaymentRequest request)
		{
			var endpoint = _configuration["Momo:ApiUrl"] ?? throw new InvalidOperationException("Momo ApiUrl configuration is missing.");
			var partnerCode = _configuration["Momo:PartnerCode"] ?? throw new InvalidOperationException("Momo PartnerCode configuration is missing.");
			var accessKey = _configuration["Momo:AccessKey"] ?? throw new InvalidOperationException("Momo AccessKey configuration is missing.");
			var secretKey = _configuration["Momo:SecretKey"] ?? throw new InvalidOperationException("Momo SecretKey configuration is missing.");
			var requestType = _configuration["Momo:RequestType"] ?? throw new InvalidOperationException("Momo RequestType configuration is missing.");

			if (string.IsNullOrWhiteSpace(partnerCode) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
			{
				throw new InvalidOperationException("Momo configuration is missing required keys.");
			}

			var returnUrl = _webHostEnvironment.EnvironmentName == Environments.Development
				? _configuration["Momo:ReturnUrl"]
				: _configuration["Momo:ReturnUrlProduct"];

			var notifyUrl = _configuration["Momo:NotifyUrl"] ?? returnUrl;

			var orderId = request.OrderId.ToString();
			var requestId = request.PaymentId.ToString("N");
			var orderInfo = $"Thanh toan don hang: {request.OrderCode}";
			var amountLong = (long)request.Amount;
			var extraData = Convert.ToBase64String(Encoding.UTF8.GetBytes(request.OrderId.ToString()));

			var rawHash = $"accessKey={accessKey}&amount={amountLong}&extraData={extraData}&ipnUrl={notifyUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={returnUrl}&requestId={requestId}&requestType={requestType}";
			var signature = ComputeHmacSha256(rawHash, secretKey);

			var requestBody = new
			{
				partnerCode,
				partnerName = "PerfumeGPT",
				storeId = "PerfumeGPT",
				requestId,
				amount = amountLong,
				orderId,
				orderInfo,
				redirectUrl = returnUrl,
				ipnUrl = notifyUrl,
				lang = "vi",
				extraData,
				requestType,
				signature
			};

			var client = _httpClientFactory.CreateClient();
			var response = await client.PostAsync(
				endpoint,
				new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

			var responseContent = await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
			{
				throw new InvalidOperationException($"Momo HTTP error: {responseContent}");
			}

			using var json = JsonDocument.Parse(responseContent);
			var root = json.RootElement;
			var resultCode = root.TryGetProperty("resultCode", out var resultCodeElement) ? resultCodeElement.GetInt32() : -1;
			var payUrl = root.TryGetProperty("payUrl", out var payUrlElement) ? payUrlElement.GetString() : null;
			var message = root.TryGetProperty("message", out var messageElement) ? messageElement.GetString() : "Unknown error";

			if (resultCode != 0 || string.IsNullOrWhiteSpace(payUrl))
			{
				throw new InvalidOperationException($"Momo create payment failed: {message}");
			}

			return new OrderCheckoutResponse
			{
				OrderId = request.OrderId,
				PaymentUrl = payUrl
			};
		}

		public MomoPaymentResponse GetPaymentResponseAsync(IQueryCollection queryParameters)
		{
			var accessKey = _configuration["Momo:AccessKey"] ?? throw new InvalidOperationException("Momo AccessKey configuration is missing.");
			var secretKey = _configuration["Momo:SecretKey"] ?? throw new InvalidOperationException("Momo SecretKey configuration is missing.");

			var partnerCode = queryParameters.TryGetValue("partnerCode", out var partnerCodeVal) ? partnerCodeVal.ToString() : string.Empty;
			var orderId = queryParameters.TryGetValue("orderId", out var orderIdVal) ? orderIdVal.ToString() : string.Empty;
			var requestId = queryParameters.TryGetValue("requestId", out var requestIdVal) ? requestIdVal.ToString() : string.Empty;
			var amountRaw = queryParameters.TryGetValue("amount", out var amountVal) ? amountVal.ToString() : "0";
			var orderInfo = queryParameters.TryGetValue("orderInfo", out var orderInfoVal) ? orderInfoVal.ToString() : string.Empty;
			var orderType = queryParameters.TryGetValue("orderType", out var orderTypeVal) ? orderTypeVal.ToString() : string.Empty;
			var transId = queryParameters.TryGetValue("transId", out var transIdVal) ? transIdVal.ToString() : string.Empty;
			var resultCode = queryParameters.TryGetValue("resultCode", out var resultCodeVal) ? resultCodeVal.ToString() : string.Empty;
			var message = queryParameters.TryGetValue("message", out var messageVal) ? messageVal.ToString() : string.Empty;
			var payType = queryParameters.TryGetValue("payType", out var payTypeVal) ? payTypeVal.ToString() : string.Empty;
			var responseTime = queryParameters.TryGetValue("responseTime", out var responseTimeVal) ? responseTimeVal.ToString() : string.Empty;
			var extraData = queryParameters.TryGetValue("extraData", out var extraDataVal) ? extraDataVal.ToString() : string.Empty;

			var inputSignature = queryParameters.TryGetValue("signature", out var sigVal) ? sigVal.ToString() : string.Empty;

			if (!Guid.TryParse(requestId, out var paymentId))
			{
				return new MomoPaymentResponse
				{
					IsSuccess = false,
					Message = "Invalid requestId (PaymentId) in MoMo response"
				};
			}

			var rawHash = $"accessKey={accessKey}&amount={amountRaw}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";

			var expectedSignature = ComputeHmacSha256(rawHash, secretKey);

			if (!string.Equals(expectedSignature, inputSignature, StringComparison.OrdinalIgnoreCase))
			{
				return new MomoPaymentResponse
				{
					IsSuccess = false,
					Message = "Security Breach: Signature validation failed.",
					PaymentId = paymentId,
					ResultCode = resultCode
				};
			}

			var success = string.Equals(resultCode, "0", StringComparison.OrdinalIgnoreCase);

			return new MomoPaymentResponse
			{
				IsSuccess = success,
				Message = success ? "Payment successful" : message,
				PaymentId = paymentId,
				ResultCode = resultCode,
				TransactionNo = transId,
				Amount = decimal.TryParse(amountRaw, out var amount) ? amount : 0
			};
		}

		private static string ComputeHmacSha256(string message, string secretKey)
		{
			var keyBytes = Encoding.UTF8.GetBytes(secretKey);
			var messageBytes = Encoding.UTF8.GetBytes(message);

			using var hmac = new HMACSHA256(keyBytes);
			var hashBytes = hmac.ComputeHash(messageBytes);
			return Convert.ToHexString(hashBytes).ToLowerInvariant();
		}
	}
}
