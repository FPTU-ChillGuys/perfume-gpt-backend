using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Momos;
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
		private readonly IMomoService _momoService;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public OrderPaymentService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
			IHttpContextAccessor httpContextAccessor)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<string> CreatePaymentAndGenerateResponseAsync(Order order, decimal amount, PaymentMethod paymentMethod)
		{
			var payment = PaymentTransaction.Create(order.Id, paymentMethod, amount);

			await _unitOfWork.Payments.AddAsync(payment);

			if (paymentMethod == PaymentMethod.VnPay)
			{
				var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("Unable to access HTTP context for VnPay integration.");

				var vnPayRequest = new VnPaymentRequest
				{
					OrderId = order.Id,
					OrderCode = order.Code,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
				return checkoutResponse.PaymentUrl;
			}
			else if (paymentMethod == PaymentMethod.Momo)
			{
				var httpContext = _httpContextAccessor.HttpContext ?? throw AppException.Internal("Unable to access HTTP context for Momo integration.");

				var momoRequest = new MomoPaymentRequest
				{
					OrderId = order.Id,
					OrderCode = order.Code,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _momoService.CreatePaymentUrlAsync(httpContext, momoRequest);
				return checkoutResponse.PaymentUrl;
			}

			return payment.Id.ToString();
		}
	}
}
