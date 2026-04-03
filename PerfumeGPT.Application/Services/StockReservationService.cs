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

		public async Task ReserveStockForOrderAsync(Guid orderId, List<(Guid VariantId, int Quantity)> items, DateTime expiresAt)
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

		public async Task ReleaseReservationAsync(Guid orderId)
		{
			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(orderId);
			if (!reservations.Any()) return;

			var reservationsGroupedByVariant = reservations
				   .Where(r => r.Status == ReservationStatus.Reserved)
				   .GroupBy(r => r.VariantId);

			foreach (var group in reservationsGroupedByVariant)
			{
				var variantId = group.Key;
				var totalQuantityToRelease = group.Sum(r => r.ReservedQuantity);

				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
				if (stock != null)
				{
					stock.ReleaseReservation(totalQuantityToRelease);
					_unitOfWork.Stocks.Update(stock);
				}

				foreach (var reservation in group)
				{
					var batch = reservation.Batch;
					batch.Release(reservation.ReservedQuantity);
					_unitOfWork.Batches.Update(batch);

					reservation.Release();
					_unitOfWork.StockReservations.Update(reservation);
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
