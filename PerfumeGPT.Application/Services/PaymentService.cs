using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.VNPays;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class PaymentService : IPaymentService
	{
		private readonly IVnPayService _vnPayService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILoyaltyPointService _loyaltyPointService;
		private readonly IReceiptRepository _receiptRepository;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public PaymentService(IVnPayService vnPayService, IUnitOfWork unitOfWork, ILoyaltyPointService loyaltyPointService, IReceiptRepository receiptRepository, IHttpContextAccessor httpContextAccessor)
		{
			_vnPayService = vnPayService;
			_unitOfWork = unitOfWork;
			_loyaltyPointService = loyaltyPointService;
			_receiptRepository = receiptRepository;
			_httpContextAccessor = httpContextAccessor;
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

		public async Task<BaseResponse<string>> ChangePaymentMethodAsync(Guid paymentId, PaymentInformation newMethod)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var currentPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
					if (currentPayment == null)
					{
						return BaseResponse<string>.Fail("Payment record not found.", ResponseErrorType.NotFound);
					}

					if (currentPayment.TransactionStatus == TransactionStatus.Success)
					{
						return BaseResponse<string>.Fail("Cannot change payment method for completed payments.", ResponseErrorType.BadRequest);
					}

					if (currentPayment.Method == newMethod.Method)
					{
						return BaseResponse<string>.Fail("New payment method is the same as current method.", ResponseErrorType.BadRequest);
					}

					if (currentPayment.TransactionStatus != TransactionStatus.Pending)
					{
						return BaseResponse<string>.Fail("Only pending payments can change payment methods. Use RetryPaymentWithMethod for failed payments.", ResponseErrorType.BadRequest);
					}

					if (currentPayment.TransactionStatus == TransactionStatus.Pending)
					{
						currentPayment.TransactionStatus = TransactionStatus.Cancelled;
						_unitOfWork.Payments.Update(currentPayment);
						await _unitOfWork.SaveChangesAsync();
					}

					var order = await _unitOfWork.Orders.GetByIdAsync(currentPayment.OrderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					var rootPaymentId = currentPayment.OriginalPaymentId ?? currentPayment.Id;
					var retryAttempt = currentPayment.RetryAttempt + 1;

					var newPayment = new PaymentTransaction
					{
						OrderId = currentPayment.OrderId,
						Method = newMethod.Method,
						Amount = currentPayment.Amount,
						TransactionStatus = TransactionStatus.Pending,
						OriginalPaymentId = rootPaymentId,
						RetryAttempt = retryAttempt
					};

					await _unitOfWork.Payments.AddAsync(newPayment);
					order.PaymentStatus = PaymentStatus.Unpaid;
					_unitOfWork.Orders.Update(order);
					await _unitOfWork.SaveChangesAsync();

					return await GeneratePaymentResponse(newPayment, order, retryAttempt, $" (changed from {currentPayment.Method} to {newMethod})");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"An error occurred while changing payment method: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> RetryPaymentWithMethodAsync(Guid paymentId, PaymentInformation? newMethod = null)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					var originalPayment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
					if (originalPayment == null)
					{
						return BaseResponse<string>.Fail("Payment record not found.", ResponseErrorType.NotFound);
					}

					if (originalPayment.TransactionStatus != TransactionStatus.Failed)
					{
						return BaseResponse<string>.Fail("Only failed payments can be retried. Use ChangePaymentMethod to switch payment methods for pending payments.", ResponseErrorType.BadRequest);
					}

					var order = await _unitOfWork.Orders.GetByIdAsync(originalPayment.OrderId);
					if (order == null)
					{
						return BaseResponse<string>.Fail("Order not found.", ResponseErrorType.NotFound);
					}

					newMethod ??= new PaymentInformation { Method = originalPayment.Method };
					var paymentMethod = newMethod.Method;
					var rootPaymentId = originalPayment.OriginalPaymentId ?? originalPayment.Id;
					var retryAttempt = originalPayment.RetryAttempt + 1;

					var newPayment = new PaymentTransaction
					{
						OrderId = originalPayment.OrderId,
						Method = paymentMethod,
						Amount = originalPayment.Amount,
						TransactionStatus = TransactionStatus.Pending,
						OriginalPaymentId = rootPaymentId,
						RetryAttempt = retryAttempt
					};

					await _unitOfWork.Payments.AddAsync(newPayment);
					order.PaymentStatus = PaymentStatus.Unpaid;
					_unitOfWork.Orders.Update(order);
					await _unitOfWork.SaveChangesAsync();

					var methodChanged = originalPayment.Method != paymentMethod;
					var methodMessage = methodChanged ? $" (changed from {originalPayment.Method} to {paymentMethod})" : "";

					return await GeneratePaymentResponse(newPayment, order, retryAttempt, methodMessage);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"An error occurred while retrying payment: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

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
		private async Task<BaseResponse<bool>> CompleteSuccessfulPayment(PaymentTransaction payment, Order order)
		{
			payment.TransactionStatus = TransactionStatus.Success;
			order.PaymentStatus = PaymentStatus.Paid;

			if (payment.Method == PaymentMethod.CashInStore)
			{
				order.Status = OrderStatus.Delivered;
			}
			else
				order.Status = OrderStatus.Processing;

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			await _unitOfWork.SaveChangesAsync();

			var existingReceipt = await _receiptRepository.GetByTransactionIdAsync(payment.Id);
			if (existingReceipt == null)
			{
				var receipt = new Receipt
				{
					TransactionId = payment.Id,
					ReceiptNumber = GenerateReceiptNumber(),
				};

				await _receiptRepository.AddAsync(receipt);
				await _unitOfWork.SaveChangesAsync();
			}

			if (order.CustomerId.HasValue)
			{
				await _unitOfWork.Carts.ClearCartByUserIdAsync(order.CustomerId.Value);

				int pointsToAward = (int)(order.TotalAmount * 0.01m);
				if (pointsToAward > 0)
				{
					await _loyaltyPointService.PlusPointAsync(order.CustomerId.Value, pointsToAward);
				}
			}

			return BaseResponse<bool>.Ok(true, "Payment processed successfully.");
		}

		private async Task<BaseResponse<bool>> HandleFailedPayment(PaymentTransaction payment, Order order, string? reason = null)
		{
			payment.TransactionStatus = TransactionStatus.Failed;
			order.PaymentStatus = PaymentStatus.Unpaid;

			_unitOfWork.Payments.Update(payment);
			_unitOfWork.Orders.Update(order);

			await _unitOfWork.SaveChangesAsync();

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

		private string GenerateReceiptNumber() => $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
	}
}
