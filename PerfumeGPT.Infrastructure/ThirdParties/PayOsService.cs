using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PerfumeGPT.Application.DTOs.Requests.PayOs;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.PayOs;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class PayOsService : IPayOsService
	{
		private const long MaxPayOsOrderCode = 9007199254740991;
		private readonly IConfiguration _configuration;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public PayOsService(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
		{
			_configuration = configuration;
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<OrderCheckoutResponse> CreatePaymentUrlAsync(PayOsPaymentRequest request)
		{
			var clientId = _configuration["PayOs:ClientID"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs ClientID.");
			var apiKey = _configuration["PayOs:ApiKey"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs ApiKey.");
			var checksumKey = _configuration["PayOs:CheckSum"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs CheckSum.");

			var returnUrl = _webHostEnvironment.EnvironmentName == Environments.Development
				? _configuration["PayOs:ReturnUrl"]
				: _configuration["PayOs:ReturnUrlProduct"];

			var cancelUrl = _webHostEnvironment.EnvironmentName == Environments.Development
				? _configuration["PayOs:CancelUrl"]
				: _configuration["PayOs:CancelUrlProduct"];

			if (string.IsNullOrWhiteSpace(returnUrl))
			{
				throw new InvalidOperationException("PayOs ReturnUrl configuration is missing.");
			}

			if (string.IsNullOrWhiteSpace(cancelUrl))
			{
				cancelUrl = returnUrl;
			}

			returnUrl = AppendQueryParam(returnUrl, "paymentId", request.PaymentId.ToString());
			cancelUrl = AppendQueryParam(cancelUrl, "paymentId", request.PaymentId.ToString());
			if (!string.IsNullOrWhiteSpace(request.PosSessionId))
			{
				returnUrl = AppendQueryParam(returnUrl, "posSessionId", request.PosSessionId);
				cancelUrl = AppendQueryParam(cancelUrl, "posSessionId", request.PosSessionId);
			}

			var payOsClient = new PayOSClient(clientId, apiKey, checksumKey);
			var orderCode = ResolveOrderCode(request.OrderCode, request.PaymentId);
			var description = BuildDescription(request.OrderCode);

			var paymentRequest = new CreatePaymentLinkRequest
			{
				OrderCode = orderCode,
				Amount = request.Amount,
				Description = description,
				CancelUrl = cancelUrl,
				ReturnUrl = returnUrl
			};

			var response = await payOsClient.PaymentRequests.CreateAsync(paymentRequest);
			if (string.IsNullOrWhiteSpace(response?.CheckoutUrl))
			{
				throw new InvalidOperationException("Thiếu URL thanh toán PayOs.");
			}

			return new OrderCheckoutResponse
			{
				OrderId = request.OrderId,
				PaymentUrl = response.CheckoutUrl
			};
		}

		public async Task<PayOsPaymentInfoResponse> GetPaymentInfoAsync(string orderCode, Guid paymentId)
		{
			var clientId = _configuration["PayOs:ClientID"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs ClientID.");
			var apiKey = _configuration["PayOs:ApiKey"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs ApiKey.");
			var checksumKey = _configuration["PayOs:CheckSum"] ?? throw new InvalidOperationException("Thiếu cấu hình PayOs CheckSum.");

			var payOsClient = new PayOSClient(clientId, apiKey, checksumKey);
			var resolvedOrderCode = ResolveOrderCode(orderCode, paymentId);

			try
			{
				var paymentInfo = await payOsClient.PaymentRequests.GetAsync(resolvedOrderCode);
				var paymentInfoJson = JsonSerializer.Serialize(paymentInfo);
				using var jsonDocument = JsonDocument.Parse(paymentInfoJson);
				var root = jsonDocument.RootElement;

				var status = root.TryGetProperty("status", out var statusElement)
					? statusElement.GetString()
					: string.Empty;

				var paymentLinkId = root.TryGetProperty("id", out var idElement)
					? idElement.GetString()
					: null;

				var amount = root.TryGetProperty("amount", out var amountElement) && amountElement.TryGetDecimal(out var amountValue)
					? amountValue
					: 0m;

				var description = root.TryGetProperty("description", out var descriptionElement)
					? descriptionElement.GetString()
					: null;

				var returnUrl = root.TryGetProperty("returnUrl", out var returnUrlElement)
					? returnUrlElement.GetString()
					: null;

				var cancelUrl = root.TryGetProperty("cancelUrl", out var cancelUrlElement)
					? cancelUrlElement.GetString()
					: null;

				var extractedOrderCode = ExtractOrderCode(description) ?? resolvedOrderCode.ToString();
				var posSessionId = ExtractQueryParam(returnUrl, "posSessionId")
					?? ExtractQueryParam(returnUrl, "sessionId")
					?? ExtractQueryParam(cancelUrl, "posSessionId")
					?? ExtractQueryParam(cancelUrl, "sessionId");

				var isPaid = string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase);

				return new PayOsPaymentInfoResponse
				{
					IsSuccess = true,
					IsPaid = isPaid,
					OrderCode = resolvedOrderCode,
					ExtractedOrderCode = extractedOrderCode,
					PosSessionId = posSessionId,
					Amount = amount,
					Status = status,
					PaymentLinkId = paymentLinkId
				};
			}
			catch (Exception ex)
			{
				return new PayOsPaymentInfoResponse
				{
					IsSuccess = false,
					IsPaid = false,
					OrderCode = resolvedOrderCode,
					ExtractedOrderCode = resolvedOrderCode.ToString(),
					Message = ex.Message
				};
			}
		}

		private static string? ExtractOrderCode(string? source)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return null;
			}

			const string prefix = "DH ";
			var startIndex = source.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
			if (startIndex < 0)
			{
				return null;
			}

			var valueStart = startIndex + prefix.Length;
			if (valueStart >= source.Length)
			{
				return null;
			}

			var value = source[valueStart..].Trim();
			return string.IsNullOrWhiteSpace(value) ? null : value;
		}

		private static string? ExtractQueryParam(string? url, string key)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return null;
			}

			var queryIndex = url.IndexOf('?');
			if (queryIndex < 0 || queryIndex == url.Length - 1)
			{
				return null;
			}

			var queryPart = url[(queryIndex + 1)..];
			var hashIndex = queryPart.IndexOf('#');
			if (hashIndex >= 0)
			{
				queryPart = queryPart[..hashIndex];
			}

			foreach (var pair in queryPart.Split('&', StringSplitOptions.RemoveEmptyEntries))
			{
				var parts = pair.Split('=', 2);
				var paramKey = Uri.UnescapeDataString(parts[0]);

				if (!string.Equals(paramKey, key, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (parts.Length < 2)
				{
					return null;
				}

				var value = Uri.UnescapeDataString(parts[1]);
				return string.IsNullOrWhiteSpace(value) ? null : value;
			}

			return null;
		}

		private static long ResolveOrderCode(string orderCode, Guid paymentId)
		{
			if (long.TryParse(orderCode, out var parsedOrderCode) && parsedOrderCode > 0)
			{
				return NormalizePayOsOrderCode(parsedOrderCode);
			}

			var fallbackOrderCode = (long)(BitConverter.ToUInt64(paymentId.ToByteArray(), 0) % MaxPayOsOrderCode);
			if (fallbackOrderCode == 0)
			{
				fallbackOrderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds() % MaxPayOsOrderCode;
			}

			return fallbackOrderCode == 0 ? 1 : fallbackOrderCode;
		}

		private static long NormalizePayOsOrderCode(long orderCode)
		{
			var normalized = (long)(unchecked((ulong)orderCode) % MaxPayOsOrderCode);
			return normalized == 0 ? 1 : normalized;
		}

		private static string BuildDescription(string orderCode)
		{
			const int maxLength = 25;
			var description = $"DH {orderCode}";

			return description.Length <= maxLength
				? description
				: description[..maxLength];
		}

		private static string AppendQueryParam(string url, string key, string value)
		{
			var separator = url.Contains('?') ? "&" : "?";
			return $"{url}{separator}{Uri.EscapeDataString(key)}={Uri.EscapeDataString(value)}";
		}
	}
}
