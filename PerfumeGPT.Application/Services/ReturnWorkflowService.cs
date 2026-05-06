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
					if (order.Status == OrderStatus.Delivered)
					{
						order.SetStatus(OrderStatus.Returning);
					}
					break;

				// NHÓM 1: KHÁCH KHÔNG GIAO HÀNG CHO SHIPPER (HỦY YÊU CẦU)
				case ShippingStatus.Returned:
				case ShippingStatus.Cancelled:
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
							order.SetStatus(OrderStatus.Delivered); // Trả lại hàng cho khách
						}
					}
					break;

				// NHÓM 2: GHN LÀM MẤT/HƯ HỎNG HÀNG SAU KHI ĐÃ LẤY (ĐẨY THẲNG SANG HOÀN TIỀN)
				case ShippingStatus.Damaged:
				case ShippingStatus.Lost:
					if (returnRequest.Status == ReturnRequestStatus.ApprovedForReturn || returnRequest.Status == ReturnRequestStatus.Inspecting)
					{
						// 1. Ghi nhận kết quả kiểm định tự động: Duyệt hoàn toàn bộ tiền, KHÔNG NHẬP KHO
						returnRequest.RecordInspectionResult(
							approvedRefundAmount: returnRequest.RequestedRefundAmount,
							isRestocked: false, // BẮT BUỘC FALSE VÌ HÀNG ĐÃ MẤT/HƯ!
							inspectionNote: $"Hệ thống tự động duyệt hoàn tiền do GHN báo sự cố ({(newShippingStatus == ShippingStatus.Damaged ? "Hư hỏng" : "Thất lạc")}). Chờ kế toán làm việc bồi thường với GHN."
						);

						// 2. Chuyển trạng thái Order
						if (order.Status == OrderStatus.Returning)
						{
							order.SetStatus(OrderStatus.Partial_Returned); // Vì không nhập kho lại vật lý, đánh dấu Partial
						}
					}
					break;
			}

			_unitOfWork.OrderReturnRequests.Update(returnRequest);
		}
	}
}
