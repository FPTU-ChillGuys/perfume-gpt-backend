using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
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

		public async Task<string> CreatePaymentAndGenerateResponseAsync(Guid orderId, decimal amount, PaymentMethod paymentMethod)
		{
			var payment = PaymentTransaction.Create(orderId, paymentMethod, amount);

			await _unitOfWork.Payments.AddAsync(payment);

			if (paymentMethod == PaymentMethod.VnPay)
			{
				var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("Unable to access HTTP context for VnPay integration.");

				var vnPayRequest = new VnPaymentRequest
				{
					OrderId = orderId,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
				return checkoutResponse.PaymentUrl;
			}
			else if (paymentMethod == PaymentMethod.Momo)
			{
				throw AppException.BadRequest("Momo payment method is currently not supported.");
			}

			return orderId.ToString();
		}
	}
}
