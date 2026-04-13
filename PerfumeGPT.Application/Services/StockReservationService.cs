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

		public StockReservationService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public async Task ReserveStockForOrderAsync(Guid orderId, List<(Guid VariantId, int Quantity)> items, DateTime? expiresAt)
		{
			foreach (var (variantId, quantity) in items)
			{
				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound($"Stock for variant {variantId} not found.");

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
					throw AppException.Conflict($"Data inconsistency: Batches do not have enough stock for variant {variantId}. Need {quantity}, missing {remainingToReserve}.");
				}

				_unitOfWork.Stocks.Update(stock);
			}
		}

		public async Task ReserveExactBatchStockForOrderAsync(Guid orderId, List<(Guid VariantId, Guid BatchId, int Quantity)> items, DateTime? expiresAt)
		{
			if (items == null || items.Count == 0)
			{
				throw AppException.BadRequest("At least one reservation item is required.");
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
					?? throw AppException.NotFound($"Batch {BatchId} not found.");

				if (batch.VariantId != VariantId)
					throw AppException.BadRequest($"Batch {BatchId} does not belong to variant {VariantId}.");

				if (batch.ExpiryDate <= DateTime.UtcNow)
					throw AppException.Conflict($"Batch {BatchId} has expired and cannot be reserved.");

				if (batch.AvailableInBatch < Quantity)
					throw AppException.Conflict($"Insufficient quantity in batch {BatchId}. Available: {batch.AvailableInBatch}, requested: {Quantity}.");

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
					?? throw AppException.NotFound($"Stock for variant {item.VariantId} not found.");

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
			if (!reservations.Any()) throw AppException.NotFound($"No reservations found for order {orderId}.");

			// Group reservations by VariantId to minimize database calls when updating Stock
			var reservationsGroupedByVariant = reservations
				.Where(r => r.Status == ReservationStatus.Reserved)
				.GroupBy(r => r.VariantId);

			foreach (var group in reservationsGroupedByVariant)
			{
				var variantId = group.Key;
				var totalQuantityToCommit = group.Sum(r => r.ReservedQuantity);

				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound($"Stock for variant {variantId} not found.");

				// 1. Release Reservation
				stock.ReleaseReservation(totalQuantityToCommit);

				// 2. Decrease real quantity in Stock
				stock.Decrease(totalQuantityToCommit);

				_unitOfWork.Stocks.Update(stock);

				foreach (var reservation in group)
				{
					var batch = reservation.Batch;

					// Do the same for Batch: release reservation and decrease quantity
					batch.Release(reservation.ReservedQuantity);
					batch.DecreaseQuantity(reservation.ReservedQuantity);

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
					}
					else if (reservation.Status == ReservationStatus.Committed)
					{
						// TRƯỜNG HỢP 2: ĐƠN ĐÃ FULFILL -> Đã bị trừ vật lý -> Phải cộng lại (Increase/Restock)
						stock?.Increase(reservation.ReservedQuantity);
						batch.IncreaseQuantity(reservation.ReservedQuantity);
					}

					_unitOfWork.Batches.Update(batch);

					// Đánh dấu phiếu này đã được giải phóng
					reservation.Release();
					_unitOfWork.StockReservations.Update(reservation);
				}

				if (stock != null)
				{
					_unitOfWork.Stocks.Update(stock);
				}
			}
		}

		public async Task<int> ProcessExpiredReservationsAsync()
		{
			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var expiredReservations = await _unitOfWork.StockReservations.GetExpiredReservationsAsync();

				if (!expiredReservations.Any())
					return 0;

				var variantIds = expiredReservations.Select(r => r.VariantId).Distinct().ToList();
				var reservedByVariant = new Dictionary<Guid, int>();
				var affectedOrders = new HashSet<Guid>();
				var count = 0;

				foreach (var reservation in expiredReservations)
				{
					if (reservation.Status != ReservationStatus.Reserved) continue;
					if (reservation.Order != null && reservation.Order.PaymentStatus == PaymentStatus.Paid) continue;

					if (!reservedByVariant.ContainsKey(reservation.VariantId))
						reservedByVariant[reservation.VariantId] = 0;

					reservedByVariant[reservation.VariantId] += reservation.ReservedQuantity;

					reservation.Batch.Release(reservation.ReservedQuantity);
					_unitOfWork.Batches.Update(reservation.Batch);

					reservation.Release();
					_unitOfWork.StockReservations.Update(reservation);

					if (reservation.Order != null && reservation.Order.Status == OrderStatus.Pending)
					{
						affectedOrders.Add(reservation.OrderId);
					}

					count++;
				}

				if (count == 0) return 0;

				var stocksToUpdate = await _unitOfWork.Stocks
					 .GetAllAsync(s => variantIds.Contains(s.VariantId));

				foreach (var stock in stocksToUpdate)
				{
					if (reservedByVariant.TryGetValue(stock.VariantId, out var totalReserved) && totalReserved > 0)
					{
						stock.ReleaseReservation(totalReserved);
						_unitOfWork.Stocks.Update(stock);
					}
				}

				if (affectedOrders.Count != 0)
				{
					var ordersToUpdate = await _unitOfWork.Orders
						 .GetAllAsync(o => affectedOrders.Contains(o.Id) && o.Status == OrderStatus.Pending);

					var cancelledOrderIds = new List<Guid>();

					foreach (var order in ordersToUpdate)
					{
						order.SetStatus(OrderStatus.Cancelled);
						_unitOfWork.Orders.Update(order);
						cancelledOrderIds.Add(order.Id);
					}

					if (cancelledOrderIds.Count != 0)
					{
						var pendingPayments = await _unitOfWork.Payments
							.GetAllAsync(p =>
								cancelledOrderIds.Contains(p.OrderId)
								&& p.TransactionType == TransactionType.Payment
								&& p.TransactionStatus == TransactionStatus.Pending);

						foreach (var payment in pendingPayments)
						{
							payment.MarkCancelled("Order was cancelled due to expired stock reservation.");
							_unitOfWork.Payments.Update(payment);
						}
					}
				}

				return count;
			});
		}
	}
}
