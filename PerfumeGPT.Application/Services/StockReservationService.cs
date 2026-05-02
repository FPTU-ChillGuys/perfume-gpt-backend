using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class StockReservationService : IStockReservationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVoucherService _voucherService;

		public StockReservationService(IUnitOfWork unitOfWork, IVoucherService voucherService)
		{
			_unitOfWork = unitOfWork;
			_voucherService = voucherService;
		}

		public async Task ReserveStockForOrderAsync(Order order)
		{
			var bufferDays = (await _unitOfWork.StorePolicies.GetCurrentPolicyAsync())?.StopSellingBeforeExpiryDays;
			var safeDate = bufferDays.HasValue ? DateTime.UtcNow.AddDays(bufferDays.Value) : DateTime.UtcNow;

			// Kéo toàn bộ Clearance Promotions lên RAM để check FEFO
			var variantIds = order.OrderDetails.Select(od => od.VariantId).Distinct().ToList();
			var activePromotions = await _unitOfWork.PromotionItems.GetAllAsync(
				pi => variantIds.Contains(pi.TargetProductVariantId) && pi.IsActive && pi.BatchId.HasValue && pi.ItemType == PromotionType.Clearance);
			var activeClearanceBatchIds = activePromotions.Select(p => p.BatchId!.Value).ToHashSet();

			var detailsByVariant = order.OrderDetails.GroupBy(od => od.VariantId);

			foreach (var group in detailsByVariant)
			{
				var variantId = group.Key;
				var totalQtyForVariant = group.Sum(od => od.Quantity);

				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}.");

				stock.Reserve(totalQtyForVariant); // 1. Trừ Stock tổng

				var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);
				var validBatchesToPick = availableBatches
					.Where(b => b.ExpiryDate > safeDate || activeClearanceBatchIds.Contains(b.Id))
					.OrderByDescending(b => activeClearanceBatchIds.Contains(b.Id))
					.ThenBy(b => b.ExpiryDate).ToList(); // 2. Lấy Batch FEFO

				// Rải Batch vào từng OrderDetail
				foreach (var orderDetail in group)
				{
					var remainingToReserve = orderDetail.Quantity;

					foreach (var batch in validBatchesToPick)
					{
						if (remainingToReserve <= 0) break;
						if (batch.AvailableInBatch <= 0) continue;

						var reserveFromBatch = Math.Min(remainingToReserve, batch.AvailableInBatch);

						// 💡 Tạo Reservation và gắn vào Detail (EF Core Auto-fixup FK)
						var reservation = new StockReservation(order.Id, batch.Id, variantId, reserveFromBatch, order.PaymentExpiresAt);
						orderDetail.AddReservation(reservation);

						batch.Reserve(reserveFromBatch);
						remainingToReserve -= reserveFromBatch;
					}

					if (remainingToReserve > 0)
						throw AppException.Conflict($"Dữ liệu không nhất quán: Các lô không đủ tồn kho cho biến thể {variantId}.");
				}

				_unitOfWork.Stocks.Update(stock);
			}
		}

		public async Task ReserveExactBatchStockForOrderAsync(Order order)
		{
			var detailsByVariant = order.OrderDetails.GroupBy(od => od.VariantId);

			foreach (var group in detailsByVariant)
			{
				var variantId = group.Key;
				var totalQtyForVariant = group.Sum(od => od.Quantity);

				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
				   ?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}.");

				stock.Reserve(totalQtyForVariant);

				foreach (var orderDetail in group)
				{
					if (!orderDetail.TransientBatchId.HasValue)
						throw AppException.BadRequest("Đơn hàng tại quầy yêu cầu mã lô cụ thể.");

					var batchId = orderDetail.TransientBatchId.Value;
					var batch = await _unitOfWork.Batches.GetByIdAsync(batchId)
					  ?? throw AppException.NotFound($"Không tìm thấy lô {batchId}.");

					if (batch.ExpiryDate <= DateTime.UtcNow)
						throw AppException.Conflict($"Lô {batch.BatchCode} đã hết hạn.");

					if (batch.AvailableInBatch < orderDetail.Quantity)
						throw AppException.Conflict($"Số lượng trong lô {batch.BatchCode} không đủ. Yêu cầu: {orderDetail.Quantity}.");

					// 💡 Tạo Reservation và gắn vào Detail
					var reservation = new StockReservation(order.Id, batch.Id, variantId, orderDetail.Quantity, order.PaymentExpiresAt);
					orderDetail.AddReservation(reservation);

					batch.Reserve(orderDetail.Quantity);
					_unitOfWork.Batches.Update(batch);
				}
				_unitOfWork.Stocks.Update(stock);
			}
		}

		public async Task CommitReservationAsync(Guid orderId)
		{
			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(orderId);
			if (!reservations.Any()) throw AppException.NotFound($"Không tìm thấy reservation cho đơn hàng {orderId}.");

			// Group reservations by VariantId to minimize database calls when updating Stock
			var reservationsGroupedByVariant = reservations
				.Where(r => r.Status == ReservationStatus.Reserved)
				.GroupBy(r => r.VariantId);

			foreach (var group in reservationsGroupedByVariant)
			{
				var variantId = group.Key;
				var totalQuantityToCommit = group.Sum(r => r.ReservedQuantity);
				var variant = await _unitOfWork.Variants.GetByIdAsync(variantId)
				  ?? throw AppException.NotFound($"Không tìm thấy biến thể {variantId}.");

				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}.");

				// 1. Release Reservation
				stock.ReleaseReservation(totalQuantityToCommit);

				// 2. Decrease real quantity in Stock
				stock.Decrease(totalQuantityToCommit);
				variant.ApplyStockPolicy(stock.TotalQuantity);

				_unitOfWork.Stocks.Update(stock);
				_unitOfWork.Variants.Update(variant);

				foreach (var reservation in group)
				{
					var batch = reservation.Batch;

					// Do the same for Batch: release reservation and decrease quantity
					batch.Release(reservation.ReservedQuantity);
					batch.DecreaseQuantity(
						 reservation.ReservedQuantity,
						 StockTransactionType.Sales,
						 orderId,
						 null,
					  $"Đã chốt tồn kho đã giữ chỗ cho đơn hàng {orderId}.");

					_unitOfWork.Batches.Update(batch);

					// Update reservation status to Committed
					reservation.Commit();
					_unitOfWork.StockReservations.Update(reservation);
				}
			}
		}

		public async Task ReleaseOrRestockCancelledOrderAsync(Guid orderId)
		{
			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(orderId);
			if (!reservations.Any()) return;

			// Lấy CẢ phiếu Reserved và phiếu Committed
			var activeReservations = reservations
				   .Where(r => r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Committed)
				   .GroupBy(r => r.VariantId);

			var variantIds = activeReservations.Select(g => g.Key).ToList();
			// Kéo tất cả Stock của các Variant này lên trong 1 câu Query
			var stocks = await _unitOfWork.Stocks.GetAllAsync(s => variantIds.Contains(s.VariantId));
			var stockDict = stocks.ToDictionary(s => s.VariantId);

			foreach (var group in activeReservations)
			{
				var stock = stockDict.GetValueOrDefault(group.Key);

				foreach (var reservation in group)
				{
					var batch = reservation.Batch;

					if (reservation.Status == ReservationStatus.Reserved)
					{
						// TRƯỜNG HỢP 1: ĐƠN CHƯA ĐÓNG GÓI -> Chỉ cần nhả số lượng giữ (Release)
						stock?.ReleaseReservation(reservation.ReservedQuantity);
						batch.Release(reservation.ReservedQuantity);
						reservation.Release();
					}
					else if (reservation.Status == ReservationStatus.Committed)
					{
						// TRƯỜNG HỢP 2: ĐƠN ĐÃ FULFILL -> Đã bị trừ vật lý -> Phải cộng lại (Increase/Restock)
						stock?.Increase(reservation.ReservedQuantity);
						batch.IncreaseQuantity(
							 reservation.ReservedQuantity,
							 StockTransactionType.Adjustment,
							 orderId,
							 null,
						   $"Nhập lại tồn kho từ reservation đã chốt do hủy đơn {orderId}.");
						reservation.Restock();
					}

					_unitOfWork.Batches.Update(batch);

					// Đánh dấu phiếu này đã được giải phóng
					_unitOfWork.StockReservations.Update(reservation);
				}

				if (stock != null)
				{
					_unitOfWork.Stocks.Update(stock);
				}
			}
		}

		public async Task<(int OrdersCleaned, int ReservationsCleaned)> CleanupExpiredOrdersAndReservationsAsync()
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var now = DateTime.UtcNow;
				int cleanedOrdersCount = 0;
				int cleanedOrphanReservationsCount = 0;
				var cleanedOrderIds = new HashSet<Guid>();

				// ==========================================
				// PHẦN 1: DỌN DẸP ĐƠN HÀNG HẾT HẠN THANH TOÁN
				// ==========================================
				var expiredOrders = await _unitOfWork.Orders.GetExpiringUnpaidOrdersAsync(50);

				foreach (var order in expiredOrders)
				{
					// 1. Hủy đơn hàng
					order.SetStatus(OrderStatus.Cancelled);
					_unitOfWork.Orders.Update(order);

					// 2. Hủy các giao dịch thanh toán đang Pending
					var pendingPayments = await _unitOfWork.Payments.GetAllAsync(
						 p => p.OrderId == order.Id
							 && p.TransactionStatus == TransactionStatus.Pending);

					foreach (var pendingPayment in pendingPayments)
					{
						pendingPayment.MarkCancelled("Đơn hàng đã bị hủy do hết hạn thanh toán.");
						_unitOfWork.Payments.Update(pendingPayment);
					}

					// 3. Hoàn trả Quota Khuyến mãi (Promotion)
					var promoUsageList = order.OrderDetails
						.Where(x => x.PromotionItemId.HasValue)
						.GroupBy(x => x.PromotionItemId!.Value)
						.Select(g => new { PromoId = g.Key, Qty = g.Sum(i => i.Quantity) });

					foreach (var usage in promoUsageList)
					{
						var promo = await _unitOfWork.PromotionItems.GetByIdAsync(usage.PromoId);
						if (promo != null)
						{
							promo.DecreaseCurrentUsage(usage.Qty);
							_unitOfWork.PromotionItems.Update(promo);
						}
					}

					// 4. Giải phóng Tồn kho
					await ReleaseOrRestockCancelledOrderAsync(order.Id);
					cleanedOrderIds.Add(order.Id);

					// 5. Hoàn Voucher lại cho khách
					await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);
					cleanedOrdersCount++;
				}

				// ==========================================
				// PHẦN 2: QUÉT RÁC RESERVATION BỊ KẸT (Orphaned)
				// ==========================================
				var orphanedReservations = await _unitOfWork.StockReservations.GetAllAsync(
					 r => r.Status == ReservationStatus.Reserved
						 && r.ExpiresAt.HasValue
						 && r.ExpiresAt.Value < now
						 // BẢO VỆ TỒN KHO CỦA ĐƠN ĐÃ CỌC/ĐÃ THANH TOÁN
						 // Chỉ nhả kho nếu Reservation này không gắn với Order nào, 
						 // HOẶC Order đó chưa trả tiền (Unpaid), HOẶC Order đó đã bị Hủy (Cancelled)
						 && (r.Order == null || r.Order.PaymentStatus == PaymentStatus.Unpaid || r.Order.Status == OrderStatus.Cancelled),
					 include: q => q.Include(r => r.Batch));

				if (orphanedReservations.Any())
				{
					var orphanedVariantIds = orphanedReservations.Select(r => r.VariantId).Distinct().ToList();
					var stocks = await _unitOfWork.Stocks.GetAllAsync(s => orphanedVariantIds.Contains(s.VariantId));
					var stockDict = stocks.ToDictionary(s => s.VariantId);

					foreach (var reservation in orphanedReservations)
					{
						if (reservation.Status != ReservationStatus.Reserved)
							continue;

						if (cleanedOrderIds.Contains(reservation.OrderId))
							continue;

						reservation.Batch.Release(reservation.ReservedQuantity);
						_unitOfWork.Batches.Update(reservation.Batch);

						if (stockDict.TryGetValue(reservation.VariantId, out var stock))
						{
							stock.ReleaseReservation(reservation.ReservedQuantity);
							_unitOfWork.Stocks.Update(stock);
						}

						reservation.Release();
						_unitOfWork.StockReservations.Update(reservation);
						cleanedOrphanReservationsCount++;
					}
				}

				return (cleanedOrdersCount, cleanedOrphanReservationsCount);
			});
		}
	}
}
