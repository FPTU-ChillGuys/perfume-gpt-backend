using Microsoft.AspNetCore.Http;
using PerfumeGPT.Application.DTOs.Requests.VNPays;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.OrderHelpers
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

		public async Task<BaseResponse<string>> CreatePaymentAndGenerateResponseAsync(
			Guid orderId,
			decimal amount,
			PaymentMethod paymentMethod,
			string successMessage)
		{
			var payment = new PaymentTransaction
			{
				OrderId = orderId,
				Method = paymentMethod,
				TransactionStatus = TransactionStatus.Pending,
				Amount = amount
			};

			await _unitOfWork.Payments.AddAsync(payment);
			// Don't save - let transaction orchestrator handle it

			if (paymentMethod == PaymentMethod.VnPay)
			{
				var httpContext = _httpContextAccessor.HttpContext;
				if (httpContext == null)
				{
					return BaseResponse<string>.Fail(
						"HttpContext is not available.",
						ResponseErrorType.InternalError);
				}

				var vnPayRequest = new VnPaymentRequest
				{
					OrderId = orderId,
					PaymentId = payment.Id,
					Amount = (int)amount
				};

				var checkoutResponse = await _vnPayService.CreatePaymentUrlAsync(httpContext, vnPayRequest);
				return BaseResponse<string>.Ok(checkoutResponse.PaymentUrl, $"{successMessage} Please complete payment.");
			}
			else if (paymentMethod == PaymentMethod.Momo)
			{
				return BaseResponse<string>.Fail("Momo payment not yet implemented.", ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(payment.Id.ToString(), successMessage);
		}
	}
}
