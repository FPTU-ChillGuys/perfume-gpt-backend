using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Base;
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

		public async Task<BaseResponse<bool>> ReserveStockForOrderAsync(
			Guid orderId,
			List<(Guid VariantId, int Quantity)> items,
			DateTime expiresAt)
		{
			try
			{
				foreach (var (variantId, quantity) in items)
				{
					// Get available batches (FIFO: ordered by ExpiryDate)
					var batches = await _unitOfWork.Batches
						.GetAvailableBatchesByVariantAsync(variantId);

					if (batches.Count == 0)
					{
						return BaseResponse<bool>.Fail(
							$"No available batches for variant {variantId}.",
							ResponseErrorType.BadRequest);
					}

					var remainingToReserve = quantity;

					foreach (var batch in batches)
					{
						if (remainingToReserve <= 0) break;

						var availableInBatch = batch.RemainingQuantity - batch.ReservedQuantity;
						if (availableInBatch <= 0) continue;

						var reserveFromBatch = Math.Min(remainingToReserve, availableInBatch);

						// Create stock reservation record
						var reservation = new StockReservation
						{
							OrderId = orderId,
							BatchId = batch.Id,
							VariantId = variantId,
							ReservedQuantity = reserveFromBatch,
							Status = ReservationStatus.Reserved,
							ExpiresAt = expiresAt
						};

						await _unitOfWork.StockReservations.AddAsync(reservation);

						// Update batch reserved quantity
						batch.ReservedQuantity += reserveFromBatch;
						_unitOfWork.Batches.Update(batch);

						remainingToReserve -= reserveFromBatch;
					}

					// Check if we could reserve enough
					if (remainingToReserve > 0)
					{
						return BaseResponse<bool>.Fail(
							$"Insufficient stock for variant {variantId}. Need {quantity}, only {quantity - remainingToReserve} available.",
							ResponseErrorType.BadRequest);
					}

					// Update stock reserved quantity
					var stock = await _unitOfWork.Stocks
						.FirstOrDefaultAsync(s => s.VariantId == variantId);

					if (stock != null)
					{
						stock.ReservedQuantity += quantity;
						_unitOfWork.Stocks.Update(stock);
					}
				}

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<bool>.Ok(true, "Stock reserved successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error reserving stock: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<bool>> CommitReservationAsync(Guid orderId)
		{
			try
			{
				// Get all reservations for this order
				var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(orderId);

				if (!reservations.Any())
				{
					return BaseResponse<bool>.Fail(
						"No reservations found for this order.",
						ResponseErrorType.NotFound);
				}

				// Calculate total reserved quantities per variant BEFORE changing status
				var variantIds = reservations.Select(r => r.VariantId).Distinct();
				var reservedByVariant = new Dictionary<Guid, int>();

				foreach (var variantId in variantIds)
				{
					var totalReserved = reservations
						.Where(r => r.VariantId == variantId && r.Status == ReservationStatus.Reserved)
						.Sum(r => r.ReservedQuantity);

					reservedByVariant[variantId] = totalReserved;
				}

				// Update batches and reservation statuses
				foreach (var reservation in reservations)
				{
					if (reservation.Status != ReservationStatus.Reserved)
					{
						continue; // Skip if already committed or released
					}

					// Update batch: decrease both reserved and remaining quantities
					var batch = reservation.Batch;
					batch.ReservedQuantity -= reservation.ReservedQuantity;
					batch.RemainingQuantity -= reservation.ReservedQuantity;
					_unitOfWork.Batches.Update(batch);

					// Update reservation status
					reservation.Status = ReservationStatus.Committed;
					_unitOfWork.StockReservations.Update(reservation);
				}

				// Update stock: decrease both reserved and total quantities using pre-calculated totals
				foreach (var variantId in variantIds)
				{
					var totalReserved = reservedByVariant[variantId];

					if (totalReserved <= 0) continue;

					var stock = await _unitOfWork.Stocks
						.FirstOrDefaultAsync(s => s.VariantId == variantId);

					if (stock != null)
					{
						stock.ReservedQuantity -= totalReserved;
						stock.TotalQuantity -= totalReserved;
						_unitOfWork.Stocks.Update(stock);
					}
				}

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<bool>.Ok(true, "Reservation committed successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error committing reservation: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<bool>> ReleaseReservationAsync(Guid orderId)
		{
			try
			{
				// Get all reservations for this order
				var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(orderId);

				if (!reservations.Any())
				{
					return BaseResponse<bool>.Ok(true, "No reservations to release.");
				}

				// Calculate total reserved quantities per variant BEFORE changing status
				var variantIds = reservations.Select(r => r.VariantId).Distinct();
				var reservedByVariant = new Dictionary<Guid, int>();

				foreach (var variantId in variantIds)
				{
					var totalReserved = reservations
						.Where(r => r.VariantId == variantId && r.Status == ReservationStatus.Reserved)
						.Sum(r => r.ReservedQuantity);

					reservedByVariant[variantId] = totalReserved;
				}

				// Update batches and reservation statuses
				foreach (var reservation in reservations)
				{
					if (reservation.Status != ReservationStatus.Reserved)
					{
						continue; // Skip if already committed or released
					}

					// Update batch: decrease reserved quantity (restore to available)
					var batch = reservation.Batch;
					batch.ReservedQuantity -= reservation.ReservedQuantity;
					_unitOfWork.Batches.Update(batch);

					// Update reservation status
					reservation.Status = ReservationStatus.Released;
					_unitOfWork.StockReservations.Update(reservation);
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
						stock.ReservedQuantity -= totalReserved;
						_unitOfWork.Stocks.Update(stock);
					}
				}

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<bool>.Ok(true, "Reservation released successfully.");
			}
			catch (Exception ex)
			{
				return BaseResponse<bool>.Fail(
					$"Error releasing reservation: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<int>> ProcessExpiredReservationsAsync()
		{
			try
			{
				var expiredReservations = await _unitOfWork.StockReservations.GetExpiredReservationsAsync();

				if (!expiredReservations.Any())
				{
					return BaseResponse<int>.Ok(0, "No expired reservations to process.");
				}

				// Calculate total reserved quantities per variant BEFORE changing status
				var variantIds = expiredReservations.Select(r => r.VariantId).Distinct();
				var reservedByVariant = new Dictionary<Guid, int>();

				foreach (var variantId in variantIds)
				{
					var totalReserved = expiredReservations
						.Where(r => r.VariantId == variantId && r.Status == ReservationStatus.Reserved)
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

					// Update batch: decrease reserved quantity
					var batch = reservation.Batch;
					batch.ReservedQuantity -= reservation.ReservedQuantity;
					_unitOfWork.Batches.Update(batch);

					reservation.Status = ReservationStatus.Released;
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
						stock.ReservedQuantity -= totalReserved;
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
						order.Status = OrderStatus.Canceled;
						_unitOfWork.Orders.Update(order);
					}
				}

				if (count > 0)
				{
					await _unitOfWork.SaveChangesAsync();
				}

				return BaseResponse<int>.Ok(count, $"Processed {count} expired reservations.");
			}
			catch (Exception ex)
			{
				return BaseResponse<int>.Fail(
					$"Error processing expired reservations: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<int> GetAvailableQuantityAsync(Guid variantId)
		{
			var stock = await _unitOfWork.Stocks
				.FirstOrDefaultAsync(s => s.VariantId == variantId);

			if (stock == null) return 0;

			return Math.Max(0, stock.TotalQuantity - stock.ReservedQuantity);
		}
	}
}
