using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderPaymentService : IOrderPaymentService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVnPayService _vnPayService;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public OrderPaymentService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IHttpContextAccessor httpContextAccessor)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<string> CreatePaymentAndGenerateResponseAsync(Order order, decimal amount, PaymentMethod paymentMethod)
		{
			var payment = PaymentTransaction.Create(order.Id, paymentMethod, amount);

			await _unitOfWork.Payments.AddAsync(payment);

			if (paymentMethod == PaymentMethod.VnPay)
			{
				payment.MarkSuccess($"MANUAL-{payment.Id:N}");
				order.MarkPaid(DateTime.UtcNow);
				order.SetStatus(OrderStatus.Pending);

				if (order.UserVoucher != null)
				{
					var userVoucher = await _unitOfWork.UserVouchers.FirstOrDefaultAsync(uv => uv.OrderId == order.Id && !uv.IsUsed);
					if (userVoucher != null)
					{
						userVoucher.MarkUsed(order.Id);
						_unitOfWork.UserVouchers.Update(userVoucher);
					}
				}

				var existingReceipt = await _unitOfWork.Receipts.FirstOrDefaultAsync(r => r.TransactionId == payment.Id);
				if (existingReceipt == null)
				{
					await _unitOfWork.Receipts.AddAsync(Receipt.Create(payment.Id));
				}

				var frontendUrl = _httpContextAccessor.HttpContext?.Request.Headers["Origin"].FirstOrDefault();
				if (string.IsNullOrWhiteSpace(frontendUrl))
				{
					frontendUrl = "http://localhost:3000";
				}

				var queryParams = new List<string>
				{
					$"orderId={order.Id}",
					$"paymentId={payment.Id}",
					$"vnp_ResponseCode=00",
					$"vnp_TxnRef={payment.Id}",
					$"vnp_Amount={Uri.EscapeDataString(((long)(amount * 100)).ToString())}",
					$"vnp_OrderInfo={Uri.EscapeDataString(order.Code)}",
					$"vnp_TransactionNo={payment.Id:N}"
				};

				return $"{frontendUrl}/payment/success?{string.Join("&", queryParams)}";
			}
			else if (paymentMethod == PaymentMethod.Momo)
			{
				throw AppException.BadRequest("Momo payment method is currently not supported.");
			}

			return order.Id.ToString();
		}
	}
}
