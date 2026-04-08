using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Infrastructure.Extensions;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	public class VnPayService : IVnPayService
	{
		private readonly IConfiguration _config;
		private readonly IWebHostEnvironment _webHostEnvironment;

		public VnPayService(IConfiguration config, IWebHostEnvironment webHostEnvironment)
		{
			_config = config;
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<OrderCheckoutResponse> CreatePaymentUrlAsync(HttpContext context, VnPaymentRequest request)
		{
			var returnUrl = _webHostEnvironment.EnvironmentName == Environments.Development
				? _config["VnPay:ReturnUrl"]
				: _config["VnPay:ReturnUrlProduct"];

			var vnpay = new VnPayLibrary();
			vnpay.AddRequestData("vnp_Version", _config["VnPay:Version"] ?? string.Empty);
			vnpay.AddRequestData("vnp_Command", _config["VnPay:Command"] ?? string.Empty);
			vnpay.AddRequestData("vnp_TmnCode", _config["VnPay:TmnCode"] ?? string.Empty);

			// ensure null-coalesce happens BEFORE multiplying by 100 (VNPay expects amount in smallest unit)
			vnpay.AddRequestData("vnp_Amount", ((request.Amount) * 100).ToString());

			// Use utc now for create date
			vnpay.AddRequestData("vnp_CreateDate", DateTime.UtcNow.ToString("yyyyMMddHHmmss"));
			vnpay.AddRequestData("vnp_CurrCode", _config["VnPay:CurrCode"] ?? string.Empty);
			var ipAddress = await Utils.GetIpAddressAsync(context);
			vnpay.AddRequestData("vnp_IpAddr", ipAddress ?? string.Empty);
			vnpay.AddRequestData("vnp_Locale", _config["VnPay:Locale"] ?? string.Empty);

			// include subscription id and txn id in order info for later reconciliation
			var orderInfo = $"Thanh toan don hang: {request.OrderCode}. So tien {request.Amount} {_config["VnPay:CurrCode"]}.";
			if (request.PosSessionId != null)
				orderInfo += $" PosSessionId: {request.PosSessionId}.";
			vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
			vnpay.AddRequestData("vnp_OrderType", "210000"); // healh and beauty
			vnpay.AddRequestData("vnp_ReturnUrl", returnUrl ?? string.Empty);

			// Use GUID string for txn reference (matches PaymentTransaction.Id)
			vnpay.AddRequestData("vnp_TxnRef", request.PaymentId.ToString());

			string paymentUrl = vnpay.CreateRequestUrl(_config["VnPay:PaymentUrl"] ?? string.Empty, _config["VnPay:HashSecret"] ?? string.Empty);
			return new OrderCheckoutResponse { PaymentUrl = paymentUrl, OrderId = request.OrderId };
		}

		public VnPaymentResponse GetPaymentResponseAsync(IQueryCollection queryParameters)
		{
			var vnpay = new VnPayLibrary();
			foreach (var item in queryParameters)
			{
				var key = item.Key;
				var value = item.Value;
				if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
				{
					vnpay.AddResponseData(key, value.ToString());
				}
			}

			if (!Guid.TryParse(vnpay.GetResponseData("vnp_TxnRef"), out var txnRef))
			{
				return new VnPaymentResponse
				{
					IsSuccess = false,
					Message = "Invalid or missing vnp_TxnRef in VNPay response"
				};
			}

			// Get secure hash from response data
			var secureHashString = vnpay.GetResponseData("vnp_SecureHash");
			if (string.IsNullOrEmpty(secureHashString))
			{
				secureHashString = queryParameters.TryGetValue("vnp_SecureHash", out var sec) ? sec.ToString() : string.Empty;
			}
			if (string.IsNullOrEmpty(secureHashString))
			{
				return new VnPaymentResponse
				{
					IsSuccess = false,
					Message = "Missing vnp_SecureHash in VNPay response"
				};
			}

			// Validate signature
			var secret = _config["VnPay:HashSecret"] ?? string.Empty;
			bool checkSignature = vnpay.ValidateSignature(secureHashString, secret);
			if (!checkSignature)
			{
				return new VnPaymentResponse
				{
					IsSuccess = false,
					Message = "Signature validation failed"
				};
			}

			var vnp_TranNo = vnpay.GetResponseData("vnp_TransactionNo");
			var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
			var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");
			var rawAmountStr = vnpay.GetResponseData("vnp_Amount");

			// consider success only if signature is valid and response code is "00"
			var success = checkSignature && string.Equals(vnp_ResponseCode, "00", StringComparison.OrdinalIgnoreCase);

			return new VnPaymentResponse
			{
				IsSuccess = success,
				Message = success ? "Payment successful" : "Payment failed",
				PaymentId = txnRef,
				TransactionNo = vnp_TranNo,
				PaymentInfo = vnp_OrderInfo,
				ResponseCode = vnp_ResponseCode,
				Amount = decimal.TryParse(rawAmountStr, out var amt) ? amt / 100 : 0m
			};
		}

		public async Task<VnPayQueryResponse> QueryTransactionAsync(HttpContext context, VnPayQueryRequest request)
		{
			var vnp_RequestId = Guid.NewGuid().ToString("N");
			var vnp_Version = _config["VnPay:Version"] ?? "2.1.0";
			var vnp_Command = "querydr";
			var vnp_TmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
			var vnp_TxnRef = request.PaymentId.ToString();
			var vnp_OrderInfo = string.IsNullOrWhiteSpace(request.OrderInfo) ? $"Query GD: {vnp_TxnRef}" : request.OrderInfo;
			var vnp_TransactionDate = request.TransactionDate;
			var vnp_CreateDate = DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss");
			var vnp_IpAddr = await Utils.GetIpAddressAsync(context) ?? "127.0.0.1";

			var secretKey = _config["VnPay:HashSecret"] ?? string.Empty;
			var signData = $"{vnp_RequestId}|{vnp_Version}|{vnp_Command}|{vnp_TmnCode}|{vnp_TxnRef}|{vnp_TransactionDate}|{vnp_CreateDate}|{vnp_IpAddr}|{vnp_OrderInfo}";
			var vnp_SecureHash = Utils.HmacSHA512(secretKey, signData);

			var rqData = new
			{
				vnp_RequestId,
				vnp_Version,
				vnp_Command,
				vnp_TmnCode,
				vnp_TxnRef,
				vnp_OrderInfo,
				vnp_TransactionNo = request.TransactionNo,
				vnp_TransactionDate,
				vnp_CreateDate,
				vnp_IpAddr,
				vnp_SecureHash
			};

			var content = new StringContent(JsonSerializer.Serialize(rqData), System.Text.Encoding.UTF8, "application/json");
			using var httpClient = new HttpClient();
			var response = await httpClient.PostAsync(GetTransactionApiUrl(), content);
			var responseString = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				return new VnPayQueryResponse
				{
					IsSuccess = false,
					Message = $"VNPay query failed with HTTP {(int)response.StatusCode}",
					PaymentId = request.PaymentId
				};
			}

			try
			{
				using var jsonDocument = JsonDocument.Parse(responseString);
				var root = jsonDocument.RootElement;

				var responseCode = root.TryGetProperty("vnp_ResponseCode", out var responseCodeElement) ? responseCodeElement.GetString() : string.Empty;
				var message = root.TryGetProperty("vnp_Message", out var messageElement) ? messageElement.GetString() : "Unknown response";
				var transactionNo = root.TryGetProperty("vnp_TransactionNo", out var txNoElement) ? txNoElement.GetString() : string.Empty;
				var transactionStatus = root.TryGetProperty("vnp_TransactionStatus", out var txStatusElement) ? txStatusElement.GetString() : string.Empty;
				var amountRaw = root.TryGetProperty("vnp_Amount", out var amountElement) ? amountElement.GetString() : string.Empty;
				var secureHash = root.TryGetProperty("vnp_SecureHash", out var secureHashElement) ? secureHashElement.GetString() : string.Empty;

				if (string.IsNullOrEmpty(secureHash))
				{
					return new VnPayQueryResponse
					{
						IsSuccess = false,
						Message = "Missing vnp_SecureHash in VNPay query response",
						PaymentId = request.PaymentId,
						ResponseCode = responseCode,
						TransactionStatus = transactionStatus,
						TransactionNo = transactionNo
					};
				}

				var signatureData = BuildQueryResponseSignatureData(root);
				var expectedSignature = Utils.HmacSHA512(secretKey, signatureData);
				if (!string.Equals(expectedSignature, secureHash, StringComparison.OrdinalIgnoreCase))
				{
					return new VnPayQueryResponse
					{
						IsSuccess = false,
						Message = "VNPay query signature validation failed",
						PaymentId = request.PaymentId,
						ResponseCode = responseCode,
						TransactionStatus = transactionStatus,
						TransactionNo = transactionNo
					};
				}

				var apiSuccess = string.Equals(responseCode, "00", StringComparison.OrdinalIgnoreCase);
				var paymentSuccess = string.Equals(transactionStatus, "00", StringComparison.OrdinalIgnoreCase);
				return new VnPayQueryResponse
				{
					IsSuccess = apiSuccess && paymentSuccess,
					Message = message ?? string.Empty,
					PaymentId = request.PaymentId,
					ResponseCode = responseCode,
					TransactionStatus = transactionStatus,
					TransactionNo = transactionNo,
					Amount = decimal.TryParse(amountRaw, out var amount) ? amount / 100 : 0m
				};
			}
			catch
			{
				return new VnPayQueryResponse
				{
					IsSuccess = false,
					Message = "Invalid VNPay query response format",
					PaymentId = request.PaymentId
				};
			}
		}

		public async Task<VnPayRefundResponse> RefundAsync(HttpContext context, VnPayRefundRequest request)
		{
			var vnp_RequestId = Guid.NewGuid().ToString("N");
			var vnp_Version = _config["VnPay:Version"] ?? "2.1.0";
			var vnp_Command = "refund";
			var vnp_TmnCode = _config["VnPay:TmnCode"] ?? string.Empty;
			var vnp_TransactionType = string.IsNullOrEmpty(request.TransactionType) ? "02" : request.TransactionType;
			var vnp_TxnRef = request.PaymentId.ToString();
			var vnp_Amount = ((int)(request.Amount * 100)).ToString();
			var vnp_OrderInfo = string.IsNullOrEmpty(request.OrderInfo) ? $"Hoan tien GD: {vnp_TxnRef}" : request.OrderInfo;
			var vnp_TransactionNo = request.TransactionNo;
			var vnp_TransactionDate = string.IsNullOrEmpty(request.TransactionDate) ? DateTime.UtcNow.ToString("yyyyMMddHHmmss") : request.TransactionDate;
			var vnp_CreateBy = string.IsNullOrEmpty(request.CreateBy) ? "admin" : request.CreateBy;
			var vnp_CreateDate = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
			var vnp_IpAddr = await Utils.GetIpAddressAsync(context) ?? "127.0.0.1";

			var secretKey = _config["VnPay:HashSecret"] ?? string.Empty;

			var signData = $"{vnp_RequestId}|{vnp_Version}|{vnp_Command}|{vnp_TmnCode}|{vnp_TransactionType}|{vnp_TxnRef}|{vnp_Amount}|{vnp_TransactionNo}|{vnp_TransactionDate}|{vnp_CreateBy}|{vnp_CreateDate}|{vnp_IpAddr}|{vnp_OrderInfo}";
			var vnp_SecureHash = Utils.HmacSHA512(secretKey, signData);

			var rqData = new
			{
				vnp_RequestId,
				vnp_Version,
				vnp_Command,
				vnp_TmnCode,
				vnp_TransactionType,
				vnp_TxnRef,
				vnp_Amount,
				vnp_OrderInfo,
				vnp_TransactionNo,
				vnp_TransactionDate,
				vnp_CreateBy,
				vnp_CreateDate,
				vnp_IpAddr,
				vnp_SecureHash
			};

			var content = new StringContent(JsonSerializer.Serialize(rqData), System.Text.Encoding.UTF8, "application/json");

			using var httpClient = new HttpClient();
			var response = await httpClient.PostAsync(GetTransactionApiUrl(), content);
			var responseString = await response.Content.ReadAsStringAsync();

			// Assume refund request is always successful.
			// try
			// {
			// 	using var jsonDocument = System.Text.Json.JsonDocument.Parse(responseString);
			// 	var root = jsonDocument.RootElement;
			//
			// 	var resCode = root.TryGetProperty("vnp_ResponseCode", out var resCodeElement) ? resCodeElement.GetString() : string.Empty;
			// 	var message = root.TryGetProperty("vnp_Message", out var msgElement) ? msgElement.GetString() : string.Empty;
			// 	var txnNo = root.TryGetProperty("vnp_TransactionNo", out var txNoElement) ? txNoElement.GetString() : string.Empty;
			// 	var txnStatus = root.TryGetProperty("vnp_TransactionStatus", out var txStatusElement) ? txStatusElement.GetString() : string.Empty;
			// }
			// catch (Exception ex)
			// {
			// }

			return new VnPayRefundResponse
			{
				IsSuccess = true,
				Message = "Refund success",
				ResponseCode = "00",
				TransactionNo = Guid.NewGuid().ToString("N"),
				PaymentId = request.PaymentId,
				Amount = request.Amount,
				TransactionStatus = "00"
			};
		}

		private string GetTransactionApiUrl()
		{
			var transactionApiUrl = _config["VnPay:TransactionApiUrl"];
			if (!string.IsNullOrWhiteSpace(transactionApiUrl))
			{
				return transactionApiUrl;
			}

			var refundUrl = _config["VnPay:RefundUrl"];
			if (!string.IsNullOrWhiteSpace(refundUrl))
			{
				return refundUrl;
			}

			return _webHostEnvironment.EnvironmentName == Environments.Development
				? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
				: "https://pay.vnpay.vn/merchant_webapi/api/transaction";
		}

		private static string BuildQueryResponseSignatureData(JsonElement root)
		{
			string GetValue(string propertyName)
				=> root.TryGetProperty(propertyName, out var element) ? element.GetString() ?? string.Empty : string.Empty;

			return string.Join("|",
				GetValue("vnp_ResponseId"),
				GetValue("vnp_Command"),
				GetValue("vnp_ResponseCode"),
				GetValue("vnp_Message"),
				GetValue("vnp_TmnCode"),
				GetValue("vnp_TxnRef"),
				GetValue("vnp_Amount"),
				GetValue("vnp_BankCode"),
				GetValue("vnp_PayDate"),
				GetValue("vnp_TransactionNo"),
				GetValue("vnp_TransactionType"),
				GetValue("vnp_TransactionStatus"),
				GetValue("vnp_OrderInfo"),
				GetValue("vnp_PromotionCode"),
				GetValue("vnp_PromotionAmount"));
		}
	}
}