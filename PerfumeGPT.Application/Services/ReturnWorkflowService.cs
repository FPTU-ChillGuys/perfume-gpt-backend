using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ReturnWorkflowService : IReturnWorkflowService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ILogger<ReturnWorkflowService> _logger;

		public ReturnWorkflowService(IUnitOfWork unitOfWork, ILogger<ReturnWorkflowService> logger)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
		}

		public async Task ProcessReturnShippingStatusAsync(Order order, OrderReturnRequest returnRequest, ShippingStatus newShippingStatus)
		{
			switch (newShippingStatus)
			{
				case ShippingStatus.Delivering:
				case ShippingStatus.Delivered:
					// GHN đã lấy được hàng từ khách và đang trên đường chở về kho
					// TODO: Đổi thành Enum tương ứng của bạn (VD: ReturningToWarehouse)
					if (order.Status == OrderStatus.Delivered)
					{
						order.SetStatus(OrderStatus.Returning);
					}
					break;

				case ShippingStatus.Returned:
				case ShippingStatus.Cancelled:
					// Kịch bản: GHN tới nhà khách gọi nhiều lần nhưng khách không đưa hàng, 
					// hoặc khách đổi ý không muốn trả hàng nữa nên GHN hủy vận đơn hoàn.
					if (returnRequest.Status == ReturnRequestStatus.ApprovedForReturn)
					{
						returnRequest.CancelBySystemWhenReturnPickupFailed("GHN không thể lấy hàng hoàn từ khách hoặc vận đơn bị hủy. Yêu cầu trả hàng đã bị hệ thống tự động hủy.");

						if (returnRequest.ReturnShipping != null)
						{
							returnRequest.ReturnShipping.Cancel();
							_unitOfWork.ShippingInfos.Update(returnRequest.ReturnShipping);
						}

						if (order.Status == OrderStatus.Returning)
						{
							order.SetStatus(OrderStatus.Delivered);
						}
					}
					break;
			}

			_unitOfWork.OrderReturnRequests.Update(returnRequest);
			// Lưu ý: Không gọi SaveChangesAsync ở đây vì sẽ được gọi tập trung ở ShippingService
		}
	}
}
