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

		public async Task ReserveStockForOrderAsync(Guid orderId, List<(Guid VariantId, int Quantity)> items, DateTime? expiresAt)
		{
			foreach (var (variantId, quantity) in items)
			{
				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {variantId}.");

				// 1. Reserve quantity in Stock 
				stock.Reserve(quantity);

				// 2. Find available batches and reserve from them
				var batches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);
				var remainingToReserve = quantity;

				foreach (var batch in batches)
				{
					if (remainingToReserve <= 0) break;

					var availableInBatch = batch.AvailableInBatch;
					if (availableInBatch <= 0) continue;

					var reserveFromBatch = Math.Min(remainingToReserve, availableInBatch);

					// Create StockReservation record
					var reservation = new StockReservation(orderId, batch.Id, variantId, reserveFromBatch, expiresAt);
					await _unitOfWork.StockReservations.AddAsync(reservation);

					// Decrease available quantity in batch
					batch.Reserve(reserveFromBatch);
					_unitOfWork.Batches.Update(batch);

					remainingToReserve -= reserveFromBatch;
				}

				// insufficient stock in batches
				if (remainingToReserve > 0)
				{
					throw AppException.Conflict($"Dữ liệu không nhất quán: Các lô không đủ tồn kho cho biến thể {variantId}. Cần {quantity}, còn thiếu {remainingToReserve}.");
				}

				_unitOfWork.Stocks.Update(stock);
			}
		}

		public async Task ReserveExactBatchStockForOrderAsync(Guid orderId, List<(Guid VariantId, Guid BatchId, int Quantity)> items, DateTime? expiresAt)
		{
			if (items == null || items.Count == 0)
			{
				throw AppException.BadRequest("Cần ít nhất một mục giữ chỗ.");
			}

			var normalizedItems = items
				.GroupBy(i => new { i.VariantId, i.BatchId })
				.Select(g => (g.Key.VariantId, g.Key.BatchId, Quantity: g.Sum(x => x.Quantity)))
				.ToList();

			// 💥 BƯỚC 1: KIỂM TRA TẤT CẢ QUYỀN TRUY CẬP VÀ SỐ LƯỢNG (Chỉ ĐỌC, không GHI)
			var batchDict = new Dictionary<Guid, Batch>();
			foreach (var (VariantId, BatchId, Quantity) in normalizedItems)
			{
				var batch = await _unitOfWork.Batches.GetByIdAsync(BatchId)
				  ?? throw AppException.NotFound($"Không tìm thấy lô {BatchId}.");

				if (batch.VariantId != VariantId)
					throw AppException.BadRequest($"Lô {BatchId} không thuộc biến thể {VariantId}.");

				if (batch.ExpiryDate <= DateTime.UtcNow)
					throw AppException.Conflict($"Lô {BatchId} đã hết hạn và không thể giữ chỗ.");

				if (batch.AvailableInBatch < Quantity)
					throw AppException.Conflict($"Số lượng trong lô {BatchId} không đủ. Khả dụng: {batch.AvailableInBatch}, yêu cầu: {Quantity}.");

				batchDict[BatchId] = batch; // Cache lại để dùng ở dưới, tránh Query DB 2 lần
			}

			// 💥 BƯỚC 2: KHI ĐÃ CHẮC CHẮN 100% CÓ ĐỦ HÀNG -> THỰC THI THAY ĐỔI
			var quantitiesByVariant = normalizedItems
				.GroupBy(i => i.VariantId)
				.Select(g => new { VariantId = g.Key, Quantity = g.Sum(x => x.Quantity) })
				.ToList();

			foreach (var item in quantitiesByVariant)
			{
				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == item.VariantId)
				   ?? throw AppException.NotFound($"Không tìm thấy tồn kho cho biến thể {item.VariantId}.");

				stock.Reserve(item.Quantity);
				_unitOfWork.Stocks.Update(stock);
			}

			foreach (var (VariantId, BatchId, Quantity) in normalizedItems)
			{
				var batch = batchDict[BatchId]; // Lấy từ Cache

				var reservation = new StockReservation(orderId, BatchId, VariantId, Quantity, expiresAt);
				await _unitOfWork.StockReservations.AddAsync(reservation);

				batch.Reserve(Quantity);
				_unitOfWork.Batches.Update(batch);
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

			foreach (var group in activeReservations)
			{
				var variantId = group.Key;
				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);

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
				// Tìm tất cả đơn hàng Pending có gài giờ hết hạn và đã lố giờ
				var expiredOrders = await _unitOfWork.Orders.GetAllAsync(
					o => o.Status == OrderStatus.Pending
						&& o.PaymentExpiresAt.HasValue
					 && o.PaymentExpiresAt.Value < now,
					include: q => q.Include(o => o.OrderDetails));

				foreach (var order in expiredOrders)
				{
					// 1. Hủy đơn hàng
					order.SetStatus(OrderStatus.Cancelled);
					_unitOfWork.Orders.Update(order);

					// 2. Hủy các giao dịch thanh toán đang Pending
					var pendingPayments = await _unitOfWork.Payments.GetAllAsync(
						 p => p.OrderId == order.Id
							 && p.TransactionStatus == TransactionStatus.Pending);

					// 3. Hoàn trả Quota Khuyến mãi (Promotion)
					foreach (var pendingPayment in pendingPayments)
					{
						pendingPayment.MarkCancelled("Đơn hàng đã bị hủy do hết hạn thanh toán.");
						_unitOfWork.Payments.Update(pendingPayment);
					}

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

					// 4. Giải phóng Tồn kho (Sử dụng lại hàm siêu xịn bạn đã nâng cấp)
					await ReleaseOrRestockCancelledOrderAsync(order.Id);
					cleanedOrderIds.Add(order.Id);

					// 5. Hoàn Voucher lại cho khách
					await _voucherService.RefundVoucherForCancelledOrderAsync(order.Id);
					cleanedOrdersCount++;
				}

				// ==========================================
				// PHẦN 2: QUÉT RÁC RESERVATION BỊ KẸT (Orphaned)
				// ==========================================
				// Đề phòng trường hợp có Reservation được tạo mà Đơn hàng bị lỗi không lưu được,
				// hoặc bị kẹt lại vì một bug nào đó trong quá khứ.
				var orphanedReservations = await _unitOfWork.StockReservations.GetAllAsync(
					 r => r.Status == ReservationStatus.Reserved
						 && r.ExpiresAt.HasValue
						 && r.ExpiresAt.Value < now,
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

						reservation.Release(); // Chuyển trạng thái sang Released
						_unitOfWork.StockReservations.Update(reservation);
						cleanedOrphanReservationsCount++;
					}
				}

				return (cleanedOrdersCount, cleanedOrphanReservationsCount);
			});
		}
	}
}
