using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
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
		private readonly IUnitOfWork _unitOfWork;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IVoucherService _voucherService;
		private readonly IEmailService _emailService;
		private readonly IEmailTemplateService _emailTemplateService;
		private readonly ILogger<PaymentService> _logger;

		public PaymentService(
			IVnPayService vnPayService,
			IUnitOfWork unitOfWork,
			IHttpContextAccessor httpContextAccessor,
			IVoucherService voucherService,
			IEmailService emailService,
			ILogger<PaymentService> logger,
			IEmailTemplateService emailTemplateService)
		{
			_vnPayService = vnPayService;
			_unitOfWork = unitOfWork;
			_httpContextAccessor = httpContextAccessor;
			_voucherService = voucherService;
			_emailService = emailService;
			_logger = logger;
			_emailTemplateService = emailTemplateService;
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
					   : await HandleFailedPayment(payment, order, request.failureReason);
			   });
		}

		// change or retry payment methods
		public async Task<BaseResponse<string>> ChangePaymentMethodAsync(Guid paymentId, PaymentInformation newMethod)
		{
			return await ProcessPaymentRetryAsync(paymentId, newMethod, requirePending: true);
		}

		public async Task<BaseResponse<string>> RetryPaymentWithMethodAsync(Guid paymentId, PaymentInformation? newMethod = null)
		{
			return await ProcessPaymentRetryAsync(paymentId, newMethod, requirePending: false);
		}

		// VnPay methods
		public async Task<BaseResponse<VnPayReturnResponse>> GetVnPayReturnResponseAsync(IQueryCollection queryParameters)
		{
			var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
			if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
			{
				throw AppException.BadRequest("Invalid VNPay response.");
			}

			var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId)
				   ?? throw AppException.NotFound("Payment record not found.");

			var orderId = payment.OrderId;

			var payload = new VnPayReturnResponse
			{
				PaymentId = payment.Id,
				OrderId = orderId
			};

			if (vnPayResponse.IsSuccess == false)
			{
				throw AppException.BadRequest("VNPay payment failed: " + vnPayResponse.Message);
			}

			return BaseResponse<VnPayReturnResponse>.Ok(payload, "VNPay payment processed successfully.");
		}

		public async Task<BaseResponse<bool>> ProcessVnPayReturnAsync(IQueryCollection queryParameters)
		{
			var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
			if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
			{
				throw AppException.BadRequest("VNPay payment failed");
			}

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId)
					   ?? throw AppException.NotFound("Payment record not found.");

				   if (!payment.IsPending())
				   {
					   return BaseResponse<bool>.Ok(true, "Payment already processed.");
				   }

				   if (payment.Amount != vnPayResponse.Amount)
				   {
					   throw AppException.BadRequest("Payment amount mismatch.");
				   }

				   var order = await _unitOfWork.Orders.GetOrderForMarkUsedVoucherAsync(payment.OrderId)
					   ?? throw AppException.NotFound("Order not found.");

				   return vnPayResponse.IsSuccess
					   ? await CompleteSuccessfulPayment(payment, order)
					   : await HandleFailedPayment(payment, order, vnPayResponse.Message);
			   });
		}

		// private methods
		private async Task<BaseResponse<string>> ProcessPaymentRetryAsync(Guid paymentId, PaymentInformation? newMethod = null, bool requirePending = false)
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			   {
				   var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId)
					   ?? throw AppException.NotFound("Payment record not found.");

				   if (currentPayment.TransactionStatus == TransactionStatus.Success)
				   {
					   throw AppException.BadRequest("Cannot retry completed payments.");
				   }

				   if (requirePending)
				   {
					   if (!currentPayment.IsPending())
					   {
						   throw AppException.BadRequest(
							   "Only pending payments can change payment methods. Use RetryPaymentWithMethod for failed payments.");
					   }

					   if (newMethod != null && currentPayment.Method == newMethod.Method)
					   {
						   throw AppException.BadRequest("New payment method is the same as current method.");
					   }
				   }
				   else if (currentPayment.TransactionStatus != TransactionStatus.Failed)
				   {
					   throw AppException.BadRequest(
						   "Only failed payments can be retried. Use ChangePaymentMethod to switch payment methods for pending payments.");
				   }

				   var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId)
					   ?? throw AppException.NotFound("Order not found.");

				   var existingPendingPayments = await _unitOfWork.Payments
					   .GetAllAsync(p => p.OrderId == order.Id &&
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

				   order.MarkUnpaid();
				   _unitOfWork.Orders.Update(order);

				   var methodChanged = currentPayment.Method != paymentMethod;
				   var methodMessage = methodChanged
					   ? $" (changed from {currentPayment.Method} to {paymentMethod})"
					   : "";

				   return await GeneratePaymentResponse(newPayment, order, newPayment.RetryAttempt, methodMessage);
			   });
		}

		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(PaymentTransaction payment, Order order)
		{
			payment.MarkSuccess();
			order.MarkPaid(DateTime.UtcNow);

			if (order.UserVoucher != null)
			{
				await _voucherService.MarkVoucherAsUsedAsync(order.Id);
			}

			if (payment.Method == PaymentMethod.CashInStore)
			{
				order.SetStatus(OrderStatus.Delivered);
			}
			else
				order.SetStatus(OrderStatus.Pending);

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			var existingReceipt = await _unitOfWork.Receipts.FirstOrDefaultAsync(r => r.TransactionId == payment.Id);
			if (existingReceipt == null)
			{
				var receipt = Receipt.Create(payment.Id);

				await _unitOfWork.Receipts.AddAsync(receipt);
			}

			try
			{
				await SendInvoiceEmailIfNeededAsync(order.Id);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Unable to send invoice email for order {OrderId}.", order.Id);
			}

			return BaseResponse<bool>.Ok(true, "Payment processed successfully.");
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(PaymentTransaction payment, Order order, string? reason = null)
		{
			payment.MarkFailed(reason);
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
						PaymentId = payment.Id,
						Amount = (int)payment.Amount
					};

					var paymentUrlResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
					return BaseResponse<string>.Ok(paymentUrlResponse.PaymentUrl, $"Payment transaction #{retryAttempt} created{methodMessage}. Redirecting to VnPay.");

				case PaymentMethod.Momo:
					throw AppException.Internal("Momo payment not yet implemented.");

				case PaymentMethod.CashOnDelivery:
				case PaymentMethod.CashInStore:
					return BaseResponse<string>.Ok(payment.Id.ToString(), $"Payment transaction #{retryAttempt} created{methodMessage}. Cash payment is pending confirmation.");

				default:
					throw AppException.BadRequest("Unsupported payment method.");
			}
		}

		private async Task SendInvoiceEmailIfNeededAsync(Guid orderId)
		{
			var payload = await _unitOfWork.Orders.GetOnlineOrderInvoiceEmailPayloadAsync(orderId);
			if (!payload.HasValue)
			{
				return;
			}

			var (customerEmail, invoice) = payload.Value;
			if (string.IsNullOrWhiteSpace(customerEmail))
			{
				return;
			}

			var subject = $"PerfumeGPT Invoice - Order {invoice.OrderId}";
			var body = _emailTemplateService.GetInvoiceTemplate(invoice);
			await _emailService.SendEmailAsync(customerEmail, subject, body);
		}
	}
}
