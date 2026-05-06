using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class OrderCancellationFinalizeService : IOrderCancellationFinalizeService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockReservationService _stockReservationService;
		private readonly IVoucherService _voucherService;

		public OrderCancellationFinalizeService(
			IUnitOfWork unitOfWork,
			IStockReservationService stockReservationService,
			IVoucherService voucherService)
		{
			_unitOfWork = unitOfWork;
			_stockReservationService = stockReservationService;
			_voucherService = voucherService;
		}

		public async Task<string?> FinalizeOrderCancellationAsync(Order order, CancelOrderReason cancelReason)
		{
			var orderToMutate = NeedsFullOrderGraph(order)
				? await _unitOfWork.Orders.GetOrderForStatusUpdateAsync(order.Id)
					?? throw AppException.NotFound("Không tìm thấy đơn hàng.")
				: order;

			var promoUsageMap = orderToMutate.OrderDetails
				.Where(x => x.PromotionItemId.HasValue)
				.GroupBy(x => x.PromotionItemId!.Value)
				.ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

			if (promoUsageMap.Count > 0)
			{
				var promoIds = promoUsageMap.Keys.ToList();
				var promotionItems = (await _unitOfWork.PromotionItems.GetAllAsync(p => promoIds.Contains(p.Id))).ToList();
				if (promotionItems.Count != promoIds.Count)
					throw AppException.BadRequest("Không tìm thấy khuyến mãi cho sản phẩm đã áp dụng.");

				foreach (var promo in promotionItems)
					promo.DecreaseCurrentUsage(promoUsageMap[promo.Id]);

				_unitOfWork.PromotionItems.UpdateRange(promotionItems);
			}

			var trackingNumberToCancel = orderToMutate.ForwardShipping?.TrackingNumber;

			if (orderToMutate.Status == OrderStatus.ReadyToPick && orderToMutate.PaymentTransactions.Any(p => p.Method == PaymentMethod.CashInStore))
				orderToMutate.CancelCashInStore(cancelReason);
			else
				orderToMutate.SetStatus(OrderStatus.Cancelled);

			foreach (var payment in orderToMutate.PaymentTransactions.Where(p => p.IsPending()))
			{
				payment.MarkCancelled("Đơn hàng đã bị hủy.");
				_unitOfWork.Payments.Update(payment);
			}

			if (orderToMutate.ForwardShipping != null)
			{
				orderToMutate.ForwardShipping.Cancel();
				_unitOfWork.ShippingInfos.Update(orderToMutate.ForwardShipping);
			}

			await _stockReservationService.ReleaseOrRestockCancelledOrderAsync(orderToMutate.Id);
			await _voucherService.RefundVoucherForCancelledOrderAsync(orderToMutate.Id);

			_unitOfWork.Orders.Update(orderToMutate);
			return trackingNumberToCancel;
		}

		private static bool NeedsFullOrderGraph(Order order)
			=> order.OrderDetails == null || order.OrderDetails.Count == 0;
	}
}
