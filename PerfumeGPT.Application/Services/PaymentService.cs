using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class PaymentService : IPaymentService
	{
		private readonly IVnPayService _vnPayService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILoyaltyPointService _loyaltyPointService;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IVoucherService _voucherService;
		private readonly IAuditScope _auditScope;
		private readonly IStockReservationService _stockReservationService;

		public PaymentService(
			IVnPayService vnPayService,
			IUnitOfWork unitOfWork,
			ILoyaltyPointService loyaltyPointService,
			IHttpContextAccessor httpContextAccessor,
			IVoucherService voucherService,
			IAuditScope auditScope,
			IStockReservationService stockReservationService)
		{
			_vnPayService = vnPayService;
			_unitOfWork = unitOfWork;
			_loyaltyPointService = loyaltyPointService;
			_httpContextAccessor = httpContextAccessor;
			_voucherService = voucherService;
			_auditScope = auditScope;
			_stockReservationService = stockReservationService;
		}

		public async Task<BaseResponse<bool>> UpdatePaymentStatusAsync(Guid paymentId, bool isSuccess, string? failureReason = null)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
					if (payment == null)
					{
						return BaseResponse<bool>.Fail("Payment record not found.", ResponseErrorType.NotFound);
					}

					if (payment.TransactionStatus == TransactionStatus.Success)
					{
						return BaseResponse<bool>.Ok(true, "Payment already processed.");
					}

					var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
					if (order == null)
					{
						return BaseResponse<bool>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					if (isSuccess)
					{
						return await CompleteSuccessfulPayment(payment, order);
					}
					else
					{
						return await HandleFailedPayment(payment, order, failureReason);
					}
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail($"An error occurred while updating payment status: {ex.Message}", ResponseErrorType.InternalError);
			}
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
				return BaseResponse<VnPayReturnResponse>.Fail("Invalid VNPay response.", ResponseErrorType.BadRequest);
			}

			var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId);
			if (payment == null)
			{
				return BaseResponse<VnPayReturnResponse>.Fail("Payment record not found.", ResponseErrorType.NotFound);
			}

			var orderId = payment.OrderId;

			var payload = new VnPayReturnResponse
			{
				PaymentId = payment.Id,
				OrderId = orderId
			};

			if (vnPayResponse.IsSuccess == false)
			{
				return new BaseResponse<VnPayReturnResponse>
				{
					Success = false,
					Message = "VNPay payment failed: " + vnPayResponse.Message,
					ErrorType = ResponseErrorType.BadRequest,
					Payload = payload
				};
			}

			return BaseResponse<VnPayReturnResponse>.Ok(payload, "VNPay payment processed successfully.");
		}

		public async Task<BaseResponse<bool>> ProcessVnPayReturnAsync(IQueryCollection queryParameters)
		{
			var vnPayResponse = _vnPayService.GetPaymentResponseAsync(queryParameters);
			if (vnPayResponse == null || vnPayResponse.PaymentId == Guid.Empty)
			{
				return BaseResponse<bool>.Fail("VNPay payment failed", ResponseErrorType.BadRequest);
			}

			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var payment = await _unitOfWork.Payments.GetByIdAsync(vnPayResponse.PaymentId);
					if (payment == null)
					{
						return BaseResponse<bool>.Fail("Payment record not found.", ResponseErrorType.NotFound);
					}

					if (payment.TransactionStatus != TransactionStatus.Pending)
					{
						return BaseResponse<bool>.Ok(true, "Payment already processed.");
					}

					if (payment.Amount != vnPayResponse.Amount)
					{
						return BaseResponse<bool>.Fail("Payment amount mismatch.", ResponseErrorType.BadRequest);
					}

					var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
					if (order == null)
					{
						return BaseResponse<bool>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					if (vnPayResponse.IsSuccess)
					{
						return await CompleteSuccessfulPayment(payment, order);
					}
					else
					{
						return await HandleFailedPayment(payment, order, vnPayResponse.Message);
					}
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail($"An error occurred while processing payment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		// private methods
		private async Task<BaseResponse<string>> ProcessPaymentRetryAsync(
			Guid paymentId,
			PaymentInformation? newMethod = null,
			bool requirePending = false)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// 1. Get current payment transaction
					var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
					if (currentPayment == null)
					{
						return BaseResponse<string>.Fail("Payment record not found.", ResponseErrorType.NotFound);
					}

					// 2. Validate payment status - cannot retry completed payments
					if (currentPayment.TransactionStatus == TransactionStatus.Success)
					{
						return BaseResponse<string>.Fail("Cannot retry completed payments.", ResponseErrorType.BadRequest);
					}

					// 3. Validate based on operation type
					if (requirePending)
					{
						// ChangePaymentMethod: only for Pending payments
						if (currentPayment.TransactionStatus != TransactionStatus.Pending)
						{
							return BaseResponse<string>.Fail(
								"Only pending payments can change payment methods. Use RetryPaymentWithMethod for failed payments.",
								ResponseErrorType.BadRequest);
						}

						// Additional validation: method must be different
						if (newMethod != null && currentPayment.Method == newMethod.Method)
						{
							return BaseResponse<string>.Fail("New payment method is the same as current method.", ResponseErrorType.BadRequest);
						}
					}
					else
					{
						// RetryPaymentWithMethod: only for Failed payments
						if (currentPayment.TransactionStatus != TransactionStatus.Failed)
						{
							return BaseResponse<string>.Fail(
								"Only failed payments can be retried. Use ChangePaymentMethod to switch payment methods for pending payments.",
								ResponseErrorType.BadRequest);
						}
					}

					// 4. Get order
					var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					// 5. Cancel any existing pending payments for this order (IMPORTANT!)
					var existingPendingPayments = await _unitOfWork.Payments
						.GetAllAsync(p => p.OrderId == order.Id &&
										  p.TransactionStatus == TransactionStatus.Pending &&
										  p.Id != paymentId);

					foreach (var pendingPayment in existingPendingPayments)
					{
						pendingPayment.TransactionStatus = TransactionStatus.Cancelled;
						_unitOfWork.Payments.Update(pendingPayment);
					}

					// 6. Update current payment status based on state
					if (currentPayment.TransactionStatus == TransactionStatus.Pending)
					{
						currentPayment.TransactionStatus = TransactionStatus.Cancelled;
						_unitOfWork.Payments.Update(currentPayment);
					}
					// If Failed, keep it as Failed for history tracking

					// 7. Determine new payment method
					var paymentMethod = newMethod?.Method ?? currentPayment.Method;

					// 8. Prepare new payment transaction
					var rootPaymentId = currentPayment.OriginalPaymentId ?? currentPayment.Id;
					var retryAttempt = currentPayment.RetryAttempt + 1;

					var newPayment = new PaymentTransaction
					{
						OrderId = currentPayment.OrderId,
						Method = paymentMethod,
						Amount = currentPayment.Amount,
						TransactionStatus = TransactionStatus.Pending,
						OriginalPaymentId = rootPaymentId,
						RetryAttempt = retryAttempt
					};

					await _unitOfWork.Payments.AddAsync(newPayment);

					// 9. Update order payment status
					order.PaymentStatus = PaymentStatus.Unpaid;
					_unitOfWork.Orders.Update(order);
					// Don't save - let transaction orchestrator handle it

					// 10. Generate response message
					var methodChanged = currentPayment.Method != paymentMethod;
					var methodMessage = methodChanged
						? $" (changed from {currentPayment.Method} to {paymentMethod})"
						: "";

					return await GeneratePaymentResponse(newPayment, order, retryAttempt, methodMessage);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error processing payment retry: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(PaymentTransaction payment, Order order)
		{
			payment.TransactionStatus = TransactionStatus.Success;
			order.PaymentStatus = PaymentStatus.Paid;
			order.PaidAt = DateTime.UtcNow;

			// Commit stock reservation for online orders (convert reserved to actual deduction)
			if (order.Type == OrderType.Online)
			{
				var commitResult = await _stockReservationService.CommitReservationAsync(order.Id);
				if (!commitResult.Success)
				{
					return BaseResponse<bool>.Fail(
						commitResult.Message ?? "Failed to commit stock reservation.",
						commitResult.ErrorType);
				}
			}

			if (order.VoucherId.HasValue)
			{
				var voucherResult = await _voucherService.MarkVoucherAsUsedAsync(order.VoucherId.Value, order.Id);
			}

			if (payment.Method == PaymentMethod.CashInStore)
			{
				order.Status = OrderStatus.Delivered;
			}
			else
				order.Status = OrderStatus.Pending;

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);
			// Don't save - let transaction orchestrator handle it

			// Create receipt if it doesn't exist
			var existingReceipt = await _unitOfWork.Receipts.FirstOrDefaultAsync(r => r.TransactionId == payment.Id);
			if (existingReceipt == null)
			{
				var receipt = new Receipt
				{
					TransactionId = payment.Id,
					ReceiptNumber = GenerateReceiptNumber(),
				};

				await _unitOfWork.Receipts.AddAsync(receipt);
				// Don't save - let transaction orchestrator handle it
			}

			// Clear cart and award loyalty points
			if (order.CustomerId.HasValue)
			{
				await _unitOfWork.Carts.ClearCartByUserIdAsync(order.CustomerId.Value);
				using (_auditScope.BeginSystemAction())
				{
					int pointsToAward = (int)(order.TotalAmount * 0.01m);
					if (pointsToAward > 0)
					{
						await _loyaltyPointService.PlusPointAsync(order.CustomerId.Value, pointsToAward);
					}
				}
			}

			// Don't save - let transaction orchestrator handle it
			return BaseResponse<bool>.Ok(true, "Payment processed successfully.");
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(PaymentTransaction payment, Order order, string? reason = null)
		{
			payment.TransactionStatus = TransactionStatus.Failed;
			order.PaymentStatus = PaymentStatus.Unpaid;

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);
			// Don't save - let transaction orchestrator handle it

			var message = string.IsNullOrEmpty(reason) ? "Payment failed." : $"Payment failed: {reason}";
			return BaseResponse<bool>.Fail(message, ResponseErrorType.BadRequest);
		}

		private async Task<BaseResponse<string>> GeneratePaymentResponse(PaymentTransaction payment, Order order, int retryAttempt, string methodMessage)
		{
			switch (payment.Method)
			{
				case PaymentMethod.VnPay:
					var httpContext = _httpContextAccessor.HttpContext;
					if (httpContext == null)
					{
						return BaseResponse<string>.Fail("HttpContext is not available.", ResponseErrorType.InternalError);
					}

					var vnPayRequest = new VnPaymentRequest
					{
						OrderId = order.Id,
						PaymentId = payment.Id,
						Amount = (int)payment.Amount
					};

					var paymentUrlResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
					return BaseResponse<string>.Ok(paymentUrlResponse.PaymentUrl, $"Payment transaction #{retryAttempt} created{methodMessage}. Redirecting to VnPay.");

				case PaymentMethod.Momo:
					return BaseResponse<string>.Fail("Momo payment not yet implemented.", ResponseErrorType.InternalError);

				case PaymentMethod.CashOnDelivery:
				case PaymentMethod.CashInStore:
					return BaseResponse<string>.Ok(payment.Id.ToString(), $"Payment transaction #{retryAttempt} created{methodMessage}. Cash payment is pending confirmation.");

				default:
					return BaseResponse<string>.Fail("Unsupported payment method.", ResponseErrorType.BadRequest);
			}
		}

		private static string GenerateReceiptNumber() => $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
	}
}
