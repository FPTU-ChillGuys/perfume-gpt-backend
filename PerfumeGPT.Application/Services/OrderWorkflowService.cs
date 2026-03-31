using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderWorkflowService : IOrderWorkflowService
	{
		private readonly ILoyaltyTransactionService _loyaltyService;
		private readonly IAuditScope _auditScope;

		public OrderWorkflowService(ILoyaltyTransactionService loyaltyService, IAuditScope auditScope)
		{
			_loyaltyService = loyaltyService;
			_auditScope = auditScope;
		}

		public async Task ProcessShippingStatusChangeAsync(Order order, ShippingStatus newShippingStatus)
		{
			switch (newShippingStatus)
			{
				case ShippingStatus.Delivered:
					order.SetStatus(OrderStatus.Delivered);

					if (order.CustomerId.HasValue)
					{
						using (_auditScope.BeginSystemAction())
						{
							int points = (int)(order.TotalAmount * 0.01m);
							if (points > 0)
								await _loyaltyService.PlusPointAsync(order.CustomerId.Value, points, order.Id, false);
						}
					}
					break;

				case ShippingStatus.Returned:
					order.SetStatus(OrderStatus.Returned);

					// Logic hoàn kho: Hàng đã về tới shop, cộng lại số lượng!
					// await _inventoryManager.RestockReturnedItemsAsync(order.Id);

					// Logic hoàn tiền (nếu đã thanh toán)
					// if (order.PaymentStatus == PaymentStatus.Paid) { ... }
					break;

				case ShippingStatus.Cancelled:
					order.SetStatus(OrderStatus.Cancelled);
					// Khách hủy trước khi lấy hàng -> Hoàn kho ngay lập tức
					// await _inventoryManager.RestockCancelledItemsAsync(order.Id);
					break;

				case ShippingStatus.Delivering:
					order.SetStatus(OrderStatus.Delivering);
					break;
			}
		}
	}
}
