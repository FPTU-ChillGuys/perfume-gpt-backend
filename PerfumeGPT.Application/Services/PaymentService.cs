using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Requests.PayOs;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.DTOs.Responses.PayOs;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class PaymentService : IPaymentService
	{
		#region Dependencies
		private const long MaxPayOsOrderCode = 9007199254740991;
		private readonly IVnPayService _vnPayService;
		private readonly IMomoService _momoService;
		private readonly IPayOsService _payOsService;
		private readonly IStockReservationService _stockReservationService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IVoucherService _voucherService;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ISignalRService _signalRService;
		private readonly ILogger<PaymentService> _logger;

		public PaymentService(
			IVnPayService vnPayService,
			IMomoService momoService,
			IPayOsService payOsService,
			IUnitOfWork unitOfWork,
			IHttpContextAccessor httpContextAccessor,
			IVoucherService voucherService,
			ILogger<PaymentService> logger,
			IBackgroundJobService backgroundJobService,
			IStockReservationService stockReservationService,
			ISignalRService signalRService)
		{
			_vnPayService = vnPayService;
			_momoService = momoService;
			_payOsService = payOsService;
			_unitOfWork = unitOfWork;
			_httpContextAccessor = httpContextAccessor;
			_voucherService = voucherService;
			_logger = logger;
			_backgroundJobService = backgroundJobService;
			_stockReservationService = stockReservationService;
			_signalRService = signalRService;
		}
		#endregion Dependencies

		public async Task<PayOsReturnResponse> ProcessPayOsReturnAsync(IQueryCollection queryParameters, bool isCancelCallback = false)
		{
			var payment = await ResolvePayOsPaymentFromCallbackAsync(queryParameters);
			var orderCode = queryParameters.TryGetValue("orderCode", out var orderCodeValue)
				? orderCodeValue.ToString()
				: string.Empty;

			var payOsInfo = await _payOsService.GetPaymentInfoAsync(orderCode, payment.Id);

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Payment record not found.");

				var payload = new PayOsReturnResponse
				{
					PaymentId = latestPayment.Id,
					OrderId = latestPayment.OrderId,
					IsSuccess = false
				};

				if (!latestPayment.IsPending())
				{
					return payload with { IsSuccess = latestPayment.TransactionStatus == TransactionStatus.Success };
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				var isCancelled = isCancelCallback ||
					(queryParameters.TryGetValue("cancel", out var cancelValue) &&
					 bool.TryParse(cancelValue.ToString(), out var parsedCancel) &&
					 parsedCancel);

				if (isCancelled)
				{
					latestPayment.MarkCancelled("PayOS payment was cancelled by user.");
					order.MarkUnpaid();
					_unitOfWork.Payments.Update(latestPayment);
					_unitOfWork.Orders.Update(order);
					return payload;
				}

				if (!payOsInfo.IsSuccess)
				{
					await HandleFailedPayment(latestPayment, order, payOsInfo.Message ?? "PayOS payment verification failed.", payOsInfo.PaymentLinkId);
					return payload;
				}

				if (payOsInfo.Amount > 0 && latestPayment.Amount != payOsInfo.Amount)
				{
					throw AppException.BadRequest("Payment amount mismatch.");
				}

				if (payOsInfo.IsPaid)
				{
					await CompleteSuccessfulPayment(latestPayment, order, payOsInfo.PaymentLinkId);
					return payload with { IsSuccess = true };
				}

				await HandleFailedPayment(latestPayment, order, $"PayOS payment status: {payOsInfo.Status ?? "UNKNOWN"}", payOsInfo.PaymentLinkId);
				return payload;
			});
		}
		public async Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, ConfirmPaymentRequest request)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Payment record not found.");

				if (!payment.IsPending())
				{
					return BaseResponse<bool>.Ok(true, "Payment already processed.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				return request.IsSuccess
					? await CompleteSuccessfulPayment(payment, order)
					: await HandleFailedPayment(payment, order, request.FailureReason);
			});
		}

		private async Task<PaymentTransaction> ResolvePayOsPaymentFromCallbackAsync(IQueryCollection queryParameters)
		{
			if (queryParameters.TryGetValue("paymentId", out var paymentIdValue) &&
				Guid.TryParse(paymentIdValue.ToString(), out var paymentId))
			{
				var paymentById = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Payment record not found.");

				if (paymentById.Method != PaymentMethod.PayOs)
				{
					throw AppException.BadRequest("Payment method is not PayOS.");
				}

				return paymentById;
			}

			if (!queryParameters.TryGetValue("orderCode", out var orderCodeValue) ||
				!long.TryParse(orderCodeValue.ToString(), out var callbackOrderCode) ||
				callbackOrderCode <= 0)
			{
				throw AppException.BadRequest("PayOS callback missing required payment identifier.");
			}

			var payOsPendingPayments = await _unitOfWork.Payments.GetAllAsync(
				p => p.Method == PaymentMethod.PayOs &&
					 p.TransactionType == TransactionType.Payment &&
					 p.TransactionStatus == TransactionStatus.Pending,
				include: q => q.Include(p => p.Order),
				orderBy: q => q.OrderByDescending(p => p.CreatedAt));

			var matchedPayment = payOsPendingPayments
				.FirstOrDefault(p => ResolvePayOsOrderCode(p.Order.Code, p.Id) == callbackOrderCode);

			return matchedPayment ?? throw AppException.NotFound("PayOS payment record not found.");
		}

		private static long ResolvePayOsOrderCode(string orderCode, Guid paymentId)
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

		public async Task<BaseResponse<string>> RetryOrChangePaymentMethodAsync(Guid paymentId, PaymentInformation? newMethod = null)
		{
			return await ProcessPaymentRetryAsync(paymentId, newMethod, newMethod?.PosSessionId);
		}

		public async Task<BaseResponse<PaymentTransactionOverviewResponse>> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request)
		{
			var response = await _unitOfWork.Payments.GetTransactionsForManagementAsync(request);
			return BaseResponse<PaymentTransactionOverviewResponse>.Ok(response);
		}

		// MoMo methods
		public async Task<MomoReturnResponse> ProcessMomoReturnAsync(IQueryCollection queryParameters)
		{
			var momoResponse = _momoService.GetPaymentResponseAsync(queryParameters);
			if (momoResponse.PaymentId == Guid.Empty)
			{
				throw AppException.BadRequest("MoMo payment failed");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(momoResponse.PaymentId)
					?? throw AppException.NotFound("Payment record not found.");

				var payload = new MomoReturnResponse
				{
					PaymentId = payment.Id,
					OrderId = payment.OrderId,
					IsSuccess = momoResponse.IsSuccess
				};

				if (!payment.IsPending())
				{
					return payload with { IsSuccess = payment.TransactionStatus == TransactionStatus.Success };
				}

				if (payment.Amount != momoResponse.Amount)
				{
					throw AppException.BadRequest("Payment amount mismatch.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				if (momoResponse.IsSuccess)
				{
					await CompleteSuccessfulPayment(payment, order, momoResponse.TransactionNo);
					return payload;
				}

				await HandleFailedPayment(payment, order, momoResponse.Message, momoResponse.TransactionNo);
				return payload;
			});
		}

		// VnPay methods
		public async Task<VnPayReturnResponse> ProcessVnPayReturnAsync(IQueryCollection queryParameters)
		{
			try
			{
				var vnPayResponse = GetValidatedVnPayResponse(queryParameters, "VNPay payment failed");
				var posSessionId = ResolvePosSessionIdFromVnPayReturn(queryParameters, vnPayResponse.PaymentInfo);

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId)
						?? throw AppException.NotFound("Payment record not found.");

					var payload = new VnPayReturnResponse
					{
						PaymentId = payment.Id,
						OrderId = payment.OrderId,
						IsSuccess = vnPayResponse.IsSuccess
					};

					if (!payment.IsPending())
					{
						return payload with { IsSuccess = payment.TransactionStatus == TransactionStatus.Success };
					}

					if (payment.Amount != vnPayResponse.Amount)
					{
						throw AppException.BadRequest("Payment amount mismatch.");
					}

					var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
						?? throw AppException.NotFound("Order not found.");

					if (vnPayResponse.IsSuccess)
					{
						await CompleteSuccessfulPayment(payment, order, vnPayResponse.TransactionNo, posSessionId);
						return payload;
					}

					await HandleFailedPayment(payment, order, vnPayResponse.Message, vnPayResponse.TransactionNo, posSessionId);
					return payload;
				});
			}
			catch (DbUpdateConcurrencyException ex)
			{
				_logger.LogWarning(ex, "Giao dịch đã được xử lý bởi một luồng khác (Khả năng cao là IPN Webhook). Bỏ qua lỗi cập nhật.");

				var vnPayResponse = GetValidatedVnPayResponse(queryParameters, "VNPay payment failed");
				var latestPayment = await _unitOfWork.Payments.FirstOrDefaultAsync(p => p.Id == vnPayResponse.PaymentId);

				if (latestPayment == null)
				{
					throw AppException.NotFound("Payment record not found.");
				}

				return new VnPayReturnResponse
				{
					PaymentId = latestPayment.Id,
					OrderId = latestPayment.OrderId,
					IsSuccess = latestPayment.TransactionStatus == TransactionStatus.Success
				};
			}
		}


		private VnPaymentResponse GetValidatedVnPayResponse(IQueryCollection queryParameters, string invalidMessage)
		{
			var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
			if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
			{
				throw AppException.BadRequest(invalidMessage);
			}

			return vnPayResponse;
		}

		// private methods
		private async Task<BaseResponse<string>> ProcessPaymentRetryAsync(Guid paymentId, PaymentInformation? newMethod = null, string? posSessionId = null)
		{
			posSessionId ??= ResolvePosSessionId();

			var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
				  ?? throw AppException.NotFound("Payment record not found.");

			if (currentPayment.TransactionStatus == TransactionStatus.Success)
				throw AppException.BadRequest("Cannot retry completed payments.");

			if (currentPayment.Method == PaymentMethod.VnPay)
			{
				var statusSyncResponse = await TryCompleteVnPayIfAlreadyPaidAsync(currentPayment);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			if (currentPayment.Method == PaymentMethod.Momo)
			{
				var statusSyncResponse = await TryCompleteMomoIfAlreadyPaidAsync(currentPayment);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			if (currentPayment.Method == PaymentMethod.PayOs)
			{
				var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				var statusSyncResponse = await TryCompletePayOsIfAlreadyPaidAsync(currentPayment, order.Code);
				if (statusSyncResponse != null)
				{
					return statusSyncResponse;
				}
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					   ?? throw AppException.NotFound("Payment record not found.");

				if (payment.TransactionStatus == TransactionStatus.Success)
					throw AppException.BadRequest("Cannot retry completed payments.");

				var isOnlineMethod = payment.Method == PaymentMethod.VnPay || payment.Method == PaymentMethod.Momo || payment.Method == PaymentMethod.PayOs;
				if (!isOnlineMethod &&
					payment.TransactionStatus != TransactionStatus.Pending &&
					payment.TransactionStatus != TransactionStatus.Failed)
					throw AppException.BadRequest("Only pending or failed payments can be retried.");

				var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId)
					 ?? throw AppException.NotFound("Order not found.");

				if (order.Status != OrderStatus.Pending)
					throw AppException.BadRequest($"Cannot change payment method or retry because the order is already being processed (Current status: {order.Status}).");

				if (order.PaymentExpiresAt.HasValue && order.PaymentExpiresAt.Value < DateTime.UtcNow)
				{
					throw AppException.BadRequest("The payment window for this order has expired. Please create a new order.");
				}

				var existingPendingPayments = await _unitOfWork.Payments
					.GetAllAsync(p => p.OrderId == order.Id &&
									  p.TransactionType == payment.TransactionType &&
									  p.TransactionStatus == TransactionStatus.Pending);

				foreach (var pendingPayment in existingPendingPayments)
				{
					pendingPayment.MarkCancelled("Superseded by new payment attempt.");
					_unitOfWork.Payments.Update(pendingPayment);
				}

				var paymentMethod = newMethod?.Method ?? payment.Method;
				var newPayment = payment.CreateRetry(paymentMethod);

				await _unitOfWork.Payments.AddAsync(newPayment);

				var refreshedExpiration = GetPaymentExpiration(paymentMethod);
				DateTime? finalExpirationToSet = order.PaymentExpiresAt;

				if (!order.PaymentExpiresAt.HasValue)
				{
					finalExpirationToSet = refreshedExpiration;
				}
				else if (refreshedExpiration.HasValue && refreshedExpiration.Value < order.PaymentExpiresAt.Value)
				{
					finalExpirationToSet = refreshedExpiration;
				}

				if (finalExpirationToSet != order.PaymentExpiresAt)
				{
					order.SetPaymentExpiration(finalExpirationToSet);

					var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
					foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Reserved))
					{
						reservation.SetExpiration(finalExpirationToSet);
						_unitOfWork.StockReservations.Update(reservation);
					}
				}

				order.MarkUnpaid();
				_unitOfWork.Orders.Update(order);

				var methodChanged = payment.Method != paymentMethod;
				var methodMessage = methodChanged
				 ? $" (changed from {payment.Method} to {paymentMethod})"
					: "";

				var paymentResponse = await GeneratePaymentResponse(newPayment, order, newPayment.RetryAttempt, methodMessage, posSessionId);

				if (!string.IsNullOrWhiteSpace(posSessionId)
					&& !string.IsNullOrWhiteSpace(paymentResponse.Payload)
					&& (paymentMethod == PaymentMethod.VnPay || paymentMethod == PaymentMethod.Momo || paymentMethod == PaymentMethod.PayOs))
				{
					try
					{
						await _signalRService.NotifyPosPaymentLinkUpdatedAsync(posSessionId, new PosPaymentLinkDto
						{
							OrderId = order.Id,
							PaymentId = newPayment.Id,
							Method = paymentMethod.ToString(),
							PaymentUrl = paymentResponse.Payload
						});
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Could not broadcast POS payment link update for order {OrderId} and session {SessionId}", order.Id, posSessionId);
					}
				}

				return paymentResponse;
			});
		}

		private async Task<BaseResponse<string>?> TryCompletePayOsIfAlreadyPaidAsync(PaymentTransaction payment, string orderCode)
		{
			var paymentInfo = await _payOsService.GetPaymentInfoAsync(orderCode, payment.Id);
			if (!paymentInfo.IsSuccess || !paymentInfo.IsPaid)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Payment record not found.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Gateway already confirmed payment.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				if (paymentInfo.Amount > 0 && latestPayment.Amount != paymentInfo.Amount)
				{
					throw AppException.BadRequest("Payment amount mismatch.");
				}

				await CompleteSuccessfulPayment(latestPayment, order, paymentInfo.PaymentLinkId);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
			});
		}

		private async Task<BaseResponse<string>?> TryCompleteMomoIfAlreadyPaidAsync(PaymentTransaction payment)
		{
			var queryRequest = new MomoQueryRequest
			{
				PaymentId = payment.Id
			};

			var queryResponse = await _momoService.QueryTransactionAsync(queryRequest);
			if (!queryResponse.IsSuccess)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Payment record not found.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Gateway already confirmed payment.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				await CompleteSuccessfulPayment(latestPayment, order, queryResponse.TransactionNo);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
			});
		}

		private async Task<BaseResponse<string>?> TryCompleteVnPayIfAlreadyPaidAsync(PaymentTransaction payment)
		{
			var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext is not available.");
			var queryRequest = new VnPayQueryRequest
			{
				PaymentId = payment.Id,
				OrderInfo = $"Query payment status before retry: {payment.Id}",
				TransactionNo = payment.GatewayTransactionNo,
				TransactionDate = payment.CreatedAt.ToString("yyyyMMddHHmmss")
			};

			var queryResponse = await _vnPayService.QueryTransactionAsync(httpContext, queryRequest);
			if (!queryResponse.IsSuccess)
			{
				return null;
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var latestPayment = await _unitOfWork.Payments.GetByIdAsync(payment.Id)
					?? throw AppException.NotFound("Payment record not found.");

				if (latestPayment.TransactionStatus == TransactionStatus.Success)
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
				}

				if (!latestPayment.IsPending())
				{
					return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Gateway already confirmed payment.");
				}

				var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(latestPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				await CompleteSuccessfulPayment(latestPayment, order, queryResponse.TransactionNo);
				return BaseResponse<string>.Ok(latestPayment.Id.ToString(), "Order was already paid successfully.");
			});
		}

		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(
			   PaymentTransaction payment,
			   Order order,
			   string? transactionNo = null,
			   string? posSessionId = null)
		{
			payment.MarkSuccess(transactionNo);
			order.MarkPaid(DateTime.UtcNow);

			if (order.UserVoucher != null)
			{
				await _voucherService.MarkVoucherAsUsedAsync(order.Id);
			}

			if (order.Type == OrderType.Offline && order.ForwardShippingId == null)
			{
				if (order.Status != OrderStatus.Delivered)
				{
					order.SetStatus(OrderStatus.Delivered);
					await _stockReservationService.CommitReservationAsync(order.Id);
				}
			}

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			var existingReceipt = await _unitOfWork.Receipts.FirstOrDefaultAsync(r => r.TransactionId == payment.Id);
			if (existingReceipt == null)
			{
				var receipt = Receipt.Create(payment.Id);
				await _unitOfWork.Receipts.AddAsync(receipt);
			}

			_backgroundJobService.EnqueueInvoiceEmail(_logger, order.Id);

			posSessionId ??= ResolvePosSessionId();
			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				var successMessage = payment.Method == PaymentMethod.VnPay
					   ? "Thanh toán VNPay thành công"
					   : "Thanh toán thành công";

				try
				{
					await _signalRService.NotifyPosPaymentCompletedAsync(posSessionId, new PosPaymentCompletedDto
					{
						OrderId = order.Id,
						PaymentId = payment.Id,
						Status = "Success",
						Message = successMessage
					});
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not broadcast POS payment completion for order {OrderId} and session {SessionId}", order.Id, posSessionId);
				}
			}

			return BaseResponse<bool>.Ok(true, "Payment processed successfully.");
		}

		private static string? ResolvePosSessionIdFromVnPayReturn(IQueryCollection queryParameters, string? paymentInfo)
		{
			if (queryParameters.TryGetValue("vnp_OrderInfo", out var orderInfoValue))
			{
				var sessionIdFromQuery = ExtractPosSessionId(orderInfoValue.ToString());
				if (!string.IsNullOrWhiteSpace(sessionIdFromQuery))
				{
					return sessionIdFromQuery;
				}
			}

			return ExtractPosSessionId(paymentInfo);
		}

		private static string? ExtractPosSessionId(string? source)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return null;
			}

			const string marker = "PosSessionId:";
			var markerIndex = source.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
			if (markerIndex < 0)
			{
				return null;
			}

			var valueStart = markerIndex + marker.Length;
			if (valueStart >= source.Length)
			{
				return null;
			}

			var tail = source[valueStart..].Trim();
			if (string.IsNullOrWhiteSpace(tail))
			{
				return null;
			}

			var dotIndex = tail.IndexOf('.');
			var rawValue = dotIndex >= 0 ? tail[..dotIndex] : tail;
			var sessionId = rawValue.Trim();

			return string.IsNullOrWhiteSpace(sessionId) ? null : sessionId;
		}

		private string? ResolvePosSessionId()
		{
			var httpContext = _httpContextAccessor.HttpContext;
			if (httpContext == null)
			{
				return null;
			}

			if (httpContext.Request.Query.TryGetValue("sessionId", out var querySessionId))
			{
				var value = querySessionId.ToString();
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}

			if (httpContext.Request.Query.TryGetValue("posSessionId", out var legacyQuerySessionId))
			{
				var value = legacyQuerySessionId.ToString();
				if (!string.IsNullOrWhiteSpace(value))
				{
					return value;
				}
			}

			return null;
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(
			PaymentTransaction payment,
			Order order,
			string? reason = null,
			string? transactionNo = null,
			string? posSessionId = null)
		{
			payment.MarkFailed(reason, transactionNo);
			order.MarkUnpaid();

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			posSessionId ??= ResolvePosSessionId();
			if (!string.IsNullOrWhiteSpace(posSessionId))
			{
				var failedMessage = string.IsNullOrWhiteSpace(reason)
					? "Thanh toán thất bại"
					: reason;

				try
				{
					await _signalRService.NotifyPosPaymentFailedAsync(posSessionId, new PosPaymentCompletedDto
					{
						OrderId = order.Id,
						PaymentId = payment.Id,
						Status = "Failed",
						Message = failedMessage
					});
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Could not broadcast POS payment failure for order {OrderId} and session {SessionId}", order.Id, posSessionId);
				}
			}

			var message = string.IsNullOrEmpty(reason) ? "Payment failed." : $"Payment failed: {reason}";
			return BaseResponse<bool>.Fail(message, ResponseErrorType.BadRequest);
		}

		private async Task<BaseResponse<string>> GeneratePaymentResponse(
			 PaymentTransaction payment,
			 Order order,
			 int retryAttempt,
			 string methodMessage,
			 string? posSessionId = null)
		{
			switch (payment.Method)
			{
				case PaymentMethod.VnPay:
					var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext is not available.");
					var vnPayRequest = new VnPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount,
						PosSessionId = posSessionId
					};

					var paymentUrlResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
					return BaseResponse<string>.Ok(paymentUrlResponse.PaymentUrl, $"Payment transaction #{retryAttempt} created{methodMessage}. Redirecting to VnPay.");

				case PaymentMethod.Momo:
					var momoHttpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("HttpContext is not available.");
					var momoRequest = new MomoPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount
					};

					var momoUrlResponse = await _momoService.CreatePaymentUrlAsync(momoHttpContext, momoRequest);
					return BaseResponse<string>.Ok(momoUrlResponse.PaymentUrl, $"Payment transaction #{retryAttempt} created{methodMessage}. Redirecting to Momo.");

				case PaymentMethod.PayOs:
					var payOsRequest = new PayOsPaymentRequest
					{
						OrderId = order.Id,
						OrderCode = order.Code,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount
					};

					var payOsUrlResponse = await _payOsService.CreatePaymentUrlAsync(payOsRequest);
					return BaseResponse<string>.Ok(payOsUrlResponse.PaymentUrl, $"Payment transaction #{retryAttempt} created{methodMessage}. Redirecting to PayOs.");

				case PaymentMethod.CashOnDelivery:
				case PaymentMethod.CashInStore:
					return BaseResponse<string>.Ok(payment.Id.ToString(), $"Payment transaction #{retryAttempt} created{methodMessage}. Cash payment is pending confirmation.");

				default:
					throw AppException.BadRequest("Unsupported payment method.");
			}
		}

		private static DateTime? GetPaymentExpiration(PaymentMethod method)
		{
			return method switch
			{
				PaymentMethod.VnPay => DateTime.UtcNow.AddMinutes(15),
				PaymentMethod.Momo => DateTime.UtcNow.AddMinutes(30),
				PaymentMethod.PayOs => DateTime.UtcNow.AddMinutes(30),
				PaymentMethod.CashOnDelivery => null,
				PaymentMethod.CashInStore => null,
				_ => null
			};
		}
	}
}