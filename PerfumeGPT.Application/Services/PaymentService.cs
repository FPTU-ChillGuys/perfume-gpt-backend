using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Momos;
using PerfumeGPT.Application.DTOs.Responses.Payments;
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
		private readonly IVnPayService _vnPayService;
		private readonly IMomoService _momoService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IVoucherService _voucherService;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<PaymentService> _logger;

		public PaymentService(
			IVnPayService vnPayService,
			IMomoService momoService,
			IUnitOfWork unitOfWork,
			IHttpContextAccessor httpContextAccessor,
			IVoucherService voucherService,
			ILogger<PaymentService> logger,
			IBackgroundJobService backgroundJobService)
		{
			_vnPayService = vnPayService;
			_momoService = momoService;
			_unitOfWork = unitOfWork;
			_httpContextAccessor = httpContextAccessor;
			_voucherService = voucherService;
			_logger = logger;
			_backgroundJobService = backgroundJobService;
		}
		#endregion Dependencies

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

				var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				return request.IsSuccess
					? await CompleteSuccessfulPayment(payment, order)
					: await HandleFailedPayment(payment, order, request.FailureReason);
			});
		}

		public async Task<BaseResponse<string>> RetryOrChangePaymentMethodAsync(Guid paymentId, PaymentInformation? newMethod = null)
		{
			return await ProcessPaymentRetryAsync(paymentId, newMethod);
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
			var vnPayResponse = GetValidatedVnPayResponse(queryParameters, "VNPay payment failed");

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
					await CompleteSuccessfulPayment(payment, order, vnPayResponse.TransactionNo);
					return payload;
				}

				await HandleFailedPayment(payment, order, vnPayResponse.Message, vnPayResponse.TransactionNo);
				return payload;
			});
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
		private async Task<BaseResponse<string>> ProcessPaymentRetryAsync(Guid paymentId, PaymentInformation? newMethod = null)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					?? throw AppException.NotFound("Payment record not found.");

				if (currentPayment.TransactionStatus == TransactionStatus.Success)
				{
					throw AppException.BadRequest("Cannot retry completed payments.");
				}

				if (currentPayment.TransactionStatus != TransactionStatus.Pending &&
					 currentPayment.TransactionStatus != TransactionStatus.Failed)
				{
					throw AppException.BadRequest("Only pending or failed payments can be retried.");
				}

				if (currentPayment.IsPending() && newMethod != null && currentPayment.Method == newMethod.Method)
				{
					throw AppException.BadRequest("New payment method is the same as current method.");
				}

				var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId)
					?? throw AppException.NotFound("Order not found.");

				var existingPendingPayments = await _unitOfWork.Payments
					.GetAllAsync(p => p.OrderId == order.Id &&
									  p.TransactionType == currentPayment.TransactionType &&
									  p.TransactionStatus == TransactionStatus.Pending &&
									  p.Id != paymentId);

				foreach (var pendingPayment in existingPendingPayments)
				{
					pendingPayment.MarkCancelled("Superseded by new payment attempt.");
					_unitOfWork.Payments.Update(pendingPayment);
				}

				if (currentPayment.IsPending())
				{
					currentPayment.MarkCancelled("Superseded by new payment attempt.");
					_unitOfWork.Payments.Update(currentPayment);
				}

				var paymentMethod = newMethod?.Method ?? currentPayment.Method;
				var newPayment = currentPayment.CreateRetry(paymentMethod);

				await _unitOfWork.Payments.AddAsync(newPayment);

				var refreshedExpiration = GetPaymentExpiration(paymentMethod);
				order.SetPaymentExpiration(refreshedExpiration);

				var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
				foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Reserved))
				{
					reservation.SetExpiration(refreshedExpiration);
					_unitOfWork.StockReservations.Update(reservation);
				}

				order.MarkUnpaid();
				_unitOfWork.Orders.Update(order);

				var methodChanged = currentPayment.Method != paymentMethod;
				var methodMessage = methodChanged
					? $" (changed from {currentPayment.Method} to {paymentMethod})"
					: "";

				return await GeneratePaymentResponse(newPayment, order, newPayment.RetryAttempt, methodMessage);
			});
		}

		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(PaymentTransaction payment, Order order, string? transactionNo = null)
		{
			payment.MarkSuccess(transactionNo);
			order.MarkPaid(DateTime.UtcNow);

			if (order.UserVoucher != null)
			{
				await _voucherService.MarkVoucherAsUsedAsync(order.Id);
			}

			if (payment.Method == PaymentMethod.CashInStore)
			{
				order.SetStatus(OrderStatus.Delivered);
			}
			if (payment.Method == PaymentMethod.VnPay)
			{
				order.SetStatus(OrderStatus.Pending);
			}
			if (payment.Method == PaymentMethod.Momo)
			{
				order.SetStatus(OrderStatus.Pending);
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

			return BaseResponse<bool>.Ok(true, "Payment processed successfully.");
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(PaymentTransaction payment, Order order, string? reason = null, string? transactionNo = null)
		{
			payment.MarkFailed(reason, transactionNo);
			order.MarkUnpaid();

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			var message = string.IsNullOrEmpty(reason) ? "Payment failed." : $"Payment failed: {reason}";
			return BaseResponse<bool>.Fail(message, ResponseErrorType.BadRequest);
		}

		private async Task<BaseResponse<string>> GeneratePaymentResponse(PaymentTransaction payment, Order order, int retryAttempt, string methodMessage)
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
						Amount = (int)payment.Amount
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

				case PaymentMethod.CashOnDelivery:
				case PaymentMethod.CashInStore:
					return BaseResponse<string>.Ok(payment.Id.ToString(), $"Payment transaction #{retryAttempt} created{methodMessage}. Cash payment is pending confirmation.");

				default:
					throw AppException.BadRequest("Unsupported payment method.");
			}
		}

		private static DateTime GetPaymentExpiration(PaymentMethod method)
		{
			return method switch
			{
				PaymentMethod.VnPay => DateTime.UtcNow.AddMinutes(15),
				PaymentMethod.Momo => DateTime.UtcNow.AddMinutes(30),
				_ => DateTime.UtcNow.AddDays(1)
			};
		}
	}
}