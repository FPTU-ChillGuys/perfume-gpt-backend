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
			var expiredReservations = await _unitOfWork.StockReservations.GetExpiredReservationsAsync();

			if (!expiredReservations.Any())
				return 0;


			// Calculate total reserved quantities per variant BEFORE changing status
			// Only count reservations that will actually be released (exclude paid orders)
			var variantIds = expiredReservations.Select(r => r.VariantId).Distinct();
			var reservedByVariant = new Dictionary<Guid, int>();

			foreach (var variantId in variantIds)
			{
				var totalReserved = expiredReservations
					.Where(r => r.VariantId == variantId
						&& r.Status == ReservationStatus.Reserved
						&& (r.Order == null || r.Order.PaymentStatus != PaymentStatus.Paid))
					.Sum(r => r.ReservedQuantity);

				reservedByVariant[variantId] = totalReserved;
			}

			// Collect affected orders for status update
			var affectedOrders = new HashSet<Guid>();
			var count = 0;

			foreach (var reservation in expiredReservations)
			{
				if (reservation.Status != ReservationStatus.Reserved)
				{
					continue;
				}

				// Skip reservations for paid orders - they are waiting for staff to package
				if (reservation.Order != null && reservation.Order.PaymentStatus == PaymentStatus.Paid)
				{
					continue;
				}

				// Update batch: decrease reserved quantity
				var batch = reservation.Batch;
				batch.Release(reservation.ReservedQuantity);
				_unitOfWork.Batches.Update(batch);

				reservation.Release();
				_unitOfWork.StockReservations.Update(reservation);

				// Track order for cancellation
				if (reservation.Order != null && reservation.Order.Status == OrderStatus.Pending)
				{
					affectedOrders.Add(reservation.OrderId);
				}

				count++;
			}

			// Update stock: decrease reserved quantity using pre-calculated totals
			foreach (var variantId in variantIds)
			{
				var totalReserved = reservedByVariant[variantId];

				if (totalReserved <= 0) continue;

				var stock = await _unitOfWork.Stocks
					.FirstOrDefaultAsync(s => s.VariantId == variantId);

				if (stock != null)
				{
					stock.ReleaseReservation(totalReserved);
					_unitOfWork.Stocks.Update(stock);
				}
			}

			// Update affected orders to Canceled status
			foreach (var orderId in affectedOrders)
			{
				var order = await _unitOfWork.Orders
					.FirstOrDefaultAsync(o => o.Id == orderId);

				if (order != null && order.Status == OrderStatus.Pending)
				{
					order.SetStatus(OrderStatus.Canceled);
					_unitOfWork.Orders.Update(order);
				}
			}

			if (count > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			return count;
		}
	}
}
