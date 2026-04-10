using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.Momos;
using PerfumeGPT.Application.DTOs.Requests.PayOs;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Payments;
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
		private readonly IPayOsService _payOsService;
		private readonly IHttpContextAccessor _httpContextAccessor;

		public OrderPaymentService(
			IUnitOfWork unitOfWork,
			IVnPayService vnPayService,
			IMomoService momoService,
		   IPayOsService payOsService,
			IHttpContextAccessor httpContextAccessor)
		{
			_unitOfWork = unitOfWork;
			_vnPayService = vnPayService;
			_momoService = momoService;
			_payOsService = payOsService;
			_httpContextAccessor = httpContextAccessor;
		}

		public async Task<CreatePaymentResponseDto> CreatePaymentAndGenerateResponseAsync(Order order, decimal amount, PaymentMethod paymentMethod, string? posSessionId)
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
					Amount = (int)amount,
					PosSessionId = posSessionId
				};

				var checkoutResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
				return new CreatePaymentResponseDto
				{
					PaymentId = payment.Id,
					PaymentUrl = checkoutResponse.PaymentUrl,
					OrderId = order.Id
				};
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
				return new CreatePaymentResponseDto
				{
					PaymentId = payment.Id,
					OrderId = order.Id,
					PaymentUrl = checkoutResponse.PaymentUrl
				};
			}
			else if (paymentMethod == PaymentMethod.PayOs)
			{
				var payOsRequest = new PayOsPaymentRequest
				{
					OrderId = order.Id,
					OrderCode = order.Code,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _payOsService.CreatePaymentUrlAsync(payOsRequest);
				return new CreatePaymentResponseDto
				{
					PaymentId = payment.Id,
					OrderId = order.Id,
					PaymentUrl = checkoutResponse.PaymentUrl
				};
			}

			return new CreatePaymentResponseDto
			{
				PaymentId = payment.Id,
				OrderId = order.Id,
				PaymentUrl = null
			};
		}
	}
}
