using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderFulfillmentService : IOrderFulfillmentService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockReservationService _stockReservationService;
		private readonly IOrderShippingHelper _shippingHelper;

		public OrderFulfillmentService(
			IUnitOfWork unitOfWork,
			IStockReservationService stockReservationService,
			IOrderShippingHelper shippingHelper)
		{
			_unitOfWork = unitOfWork;
			_stockReservationService = stockReservationService;
			_shippingHelper = shippingHelper;
		}

		#region Pick List Generation

		/// <inheritdoc />
		public async Task<BaseResponse<PickListResponse>> GetPickListAsync(Guid orderId)
		{
			try
			{
				var order = await _unitOfWork.Orders.GetByConditionAsync(
					o => o.Id == orderId,
					o => o.Include(o => o.OrderDetails));

				if (order == null)
				{
					return BaseResponse<PickListResponse>.Fail("Order not found.", ResponseErrorType.NotFound);
				}

				if (order.Type != OrderType.Online)
				{
					return BaseResponse<PickListResponse>.Fail(
						"Pick list is only available for online orders.",
						ResponseErrorType.BadRequest);
				}

				var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
				var pickListItems = await BuildPickListItemsAsync(order.OrderDetails, reservations);

				var pickListResponse = new PickListResponse
				{
					OrderId = order.Id,
					OrderCode = order.Id.ToString("N")[..8].ToUpper(),
					Items = pickListItems
				};

				return BaseResponse<PickListResponse>.Ok(pickListResponse);
			}
			catch (Exception ex)
			{
				return BaseResponse<PickListResponse>.Fail(
					$"An error occurred while generating pick list: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		private async Task<List<PickListItemResponse>> BuildPickListItemsAsync(
			ICollection<OrderDetail> orderDetails,
			IEnumerable<StockReservation> reservations)
		{
			var pickListItems = new List<PickListItemResponse>();

			var reservationsByVariant = reservations
				.Where(r => r.Status == ReservationStatus.Reserved)
				.GroupBy(r => r.VariantId)
				.ToList();

			foreach (var orderDetail in orderDetails)
			{
				var variantReservations = reservationsByVariant
					.FirstOrDefault(g => g.Key == orderDetail.VariantId);

				var batchInfoList = new List<PickListBatchInfo>();

				if (variantReservations != null)
				{
					foreach (var reservation in variantReservations)
					{
						var batch = await _unitOfWork.Batches.GetBatchByIdWithIncludesAsync(reservation.BatchId);
						if (batch != null)
						{
							batchInfoList.Add(new PickListBatchInfo
							{
								ReservationId = reservation.Id,
								BatchId = batch.Id,
								BatchCode = batch.BatchCode,
								Location = batch.ImportDetail?.Note,
								ReservedQuantity = reservation.ReservedQuantity,
								ExpiryDate = batch.ExpiryDate
							});
						}
					}
				}

				pickListItems.Add(new PickListItemResponse
				{
					OrderDetailId = orderDetail.Id,
					VariantId = orderDetail.VariantId,
					VariantName = orderDetail.Snapshot,
					Quantity = orderDetail.Quantity,
					Batches = batchInfoList
				});
			}

			return pickListItems;
		}

		#endregion

		#region Order Fulfillment

		/// <inheritdoc />
		public async Task<BaseResponse<string>> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			try
			{
				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Validate order state
					var orderValidation = await ValidateOrderForFulfillmentAsync(orderId);
					if (!orderValidation.Success || orderValidation.Payload == null)
					{
						return BaseResponse<string>.Fail(orderValidation.Message!, orderValidation.ErrorType);
					}

					var order = orderValidation.Payload;

					// Validate scanned batch codes
					var batchValidation = await ValidateScannedBatchCodesAsync(order, request);
					if (!batchValidation.Success)
					{
						return BaseResponse<string>.Fail(batchValidation.Message!, batchValidation.ErrorType);
					}

					// Commit stock reservation
					var commitResult = await _stockReservationService.CommitReservationAsync(order.Id);
					if (!commitResult.Success)
					{
						return BaseResponse<string>.Fail(
							commitResult.Message ?? "Failed to commit stock reservation.",
							commitResult.ErrorType);
					}

					// Update order status
					order.Status = OrderStatus.Shipped;
					order.StaffId = staffId;
					_unitOfWork.Orders.Update(order);

					// Handle shipping
					await ProcessShippingAsync(order);

					return BaseResponse<string>.Ok("Order fulfilled successfully. Stock committed and shipping order created.");
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"An error occurred while fulfilling order: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		private async Task<BaseResponse<Order>> ValidateOrderForFulfillmentAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetByConditionAsync(
				o => o.Id == orderId,
				o => o.Include(o => o.ShippingInfo).Include(o => o.OrderDetails));

			if (order == null)
			{
				return BaseResponse<Order>.Fail("Order not found.", ResponseErrorType.NotFound);
			}

			if (order.Status != OrderStatus.Processing)
			{
				return BaseResponse<Order>.Fail(
					$"Order must be in Processing status to fulfill. Current status: {order.Status}",
					ResponseErrorType.BadRequest);
			}

			if (order.Type != OrderType.Online)
			{
				return BaseResponse<Order>.Fail(
					"Only online orders can be fulfilled through this method.",
					ResponseErrorType.BadRequest);
			}

			return BaseResponse<Order>.Ok(order);
		}

		private async Task<BaseResponse<bool>> ValidateScannedBatchCodesAsync(Order order, FulfillOrderRequest request)
		{
			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			var activeReservations = reservations.Where(r => r.Status == ReservationStatus.Reserved).ToList();

			if (activeReservations.Count == 0)
			{
				return BaseResponse<bool>.Fail(
					"No active reservations found for this order.",
					ResponseErrorType.BadRequest);
			}

			// Validate all order details are included in the request
			var requestOrderDetailIds = request.Items.Select(i => i.OrderDetailId).ToHashSet();
			var orderDetailIds = order.OrderDetails.Select(od => od.Id).ToHashSet();

			if (!orderDetailIds.SetEquals(requestOrderDetailIds))
			{
				var missingDetails = orderDetailIds.Except(requestOrderDetailIds).ToList();
				var extraDetails = requestOrderDetailIds.Except(orderDetailIds).ToList();

				if (missingDetails.Count > 0)
				{
					return BaseResponse<bool>.Fail(
						$"Missing fulfillment items for order details: {string.Join(", ", missingDetails)}.",
						ResponseErrorType.BadRequest);
				}

				if (extraDetails.Count > 0)
				{
					return BaseResponse<bool>.Fail(
						$"Unknown order detail IDs provided: {string.Join(", ", extraDetails)}.",
						ResponseErrorType.BadRequest);
				}
			}

			foreach (var item in request.Items)
			{
				var orderDetail = order.OrderDetails.FirstOrDefault(od => od.Id == item.OrderDetailId);
				if (orderDetail == null)
				{
					return BaseResponse<bool>.Fail(
						$"Order detail {item.OrderDetailId} not found.",
						ResponseErrorType.NotFound);
				}

				// Validate quantity matches order detail quantity
				if (item.Quantity != orderDetail.Quantity)
				{
					return BaseResponse<bool>.Fail(
						$"Quantity mismatch for order detail {item.OrderDetailId}. Expected: {orderDetail.Quantity}, Provided: {item.Quantity}.",
						ResponseErrorType.BadRequest);
				}

				var variantReservations = activeReservations.Where(r => r.VariantId == orderDetail.VariantId).ToList();

				// Validate total reserved quantity matches order quantity
				var totalReservedQuantity = variantReservations.Sum(r => r.ReservedQuantity);
				if (totalReservedQuantity != orderDetail.Quantity)
				{
					return BaseResponse<bool>.Fail(
						$"Reserved quantity mismatch for order detail {item.OrderDetailId}. Reserved: {totalReservedQuantity}, Required: {orderDetail.Quantity}.",
						ResponseErrorType.BadRequest);
				}

				var matchingReservation = false;

				foreach (var reservation in variantReservations)
				{
					var batch = await _unitOfWork.Batches.GetByIdAsync(reservation.BatchId);
					if (batch != null && batch.BatchCode == item.ScannedBatchCode)
					{
						matchingReservation = true;
						break;
					}
				}

				if (!matchingReservation)
				{
					return BaseResponse<bool>.Fail(
						$"Scanned batch code '{item.ScannedBatchCode}' does not match any reserved batch for order detail {item.OrderDetailId}. Use SwapDamagedStockAsync if the item is damaged.",
						ResponseErrorType.BadRequest);
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		private async Task ProcessShippingAsync(Order order)
		{
			if (order.ShippingInfo != null)
			{
				order.ShippingInfo.Status = ShippingStatus.Shipped;
				_unitOfWork.ShippingInfos.Update(order.ShippingInfo);

				var recipientInfo = await _unitOfWork.RecipientInfos.GetByOrderIdAsync(order.Id);
				if (recipientInfo != null)
				{
					var ghnOrderResult = await _shippingHelper.CreateGHNShippingOrderAsync(order, recipientInfo);
					if (!ghnOrderResult.Success)
					{
						Console.WriteLine($"Warning: Failed to create GHN order: {ghnOrderResult.Message}");
					}
				}
			}
		}

		#endregion

		#region Damaged Stock Handling

		/// <inheritdoc />
		public async Task<BaseResponse<SwapDamagedStockResponse>> SwapDamagedStockAsync(
			Guid orderId,
			Guid staffId,
			SwapDamagedStockRequest request)
		{
			try
			{
				// Validate order and reservation BEFORE starting transaction
				var validationResult = await ValidateSwapRequestAsync(orderId, request);
				if (!validationResult.Success || validationResult.Payload == null)
				{
					return BaseResponse<SwapDamagedStockResponse>.Fail(
						validationResult.Message!,
						validationResult.ErrorType);
				}

				var (order, damagedReservation, damagedBatch) = validationResult.Payload.Value;
				var quantityToSwap = damagedReservation.ReservedQuantity;
				var variantId = damagedReservation.VariantId;

				// Step 1: Check if the SAME batch still has enough available quantity after removing damaged items
				// AvailableInBatch = RemainingQuantity - ReservedQuantity
				// After releasing the damaged reservation, available will increase by quantityToSwap
				// So we check: (AvailableInBatch + quantityToSwap) >= quantityToSwap, simplified to AvailableInBatch >= 0
				// But we need to ensure there's actually stock to reserve, so check if remaining > reserved (has unreserved stock)
				var sameBatchHasEnough = (damagedBatch.RemainingQuantity - damagedBatch.ReservedQuantity) >= quantityToSwap;

				Batch newBatch;
				bool usingSameBatch;

				if (sameBatchHasEnough)
				{
					// Same batch has enough unreserved quantity - use it
					newBatch = damagedBatch;
					usingSameBatch = true;
				}
				else
				{
					// Need to find a different batch with available quantity
					var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantAsync(variantId);
					var foundBatch = availableBatches
						.Where(b => b.Id != damagedBatch.Id && b.AvailableInBatch >= quantityToSwap)
						.OrderBy(b => b.ExpiryDate) // FEFO: First Expiry First Out
						.FirstOrDefault();

					if (foundBatch == null)
					{
						return BaseResponse<SwapDamagedStockResponse>.Fail(
							$"No available batch found with sufficient quantity ({quantityToSwap}) for variant {variantId}. Cannot swap damaged stock.",
							ResponseErrorType.BadRequest);
					}

					newBatch = foundBatch;
					usingSameBatch = false;
				}

				return await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					// Step 2: Create stock adjustment for damaged batch
					await CreateDamageAdjustmentAsync(staffId, orderId, variantId, damagedBatch.Id, quantityToSwap, request.DamageNote);

					// Step 3: Process damaged batch and release reservation
					await ProcessDamagedBatchAsync(damagedBatch, damagedReservation, quantityToSwap, variantId);

					// Step 4: Reserve the new batch (we already verified it exists)
					var newReservation = new StockReservation
					{
						OrderId = orderId,
						BatchId = newBatch.Id,
						VariantId = variantId,
						ReservedQuantity = quantityToSwap,
						Status = ReservationStatus.Reserved,
						ExpiresAt = damagedReservation.ExpiresAt
					};

					await _unitOfWork.StockReservations.AddAsync(newReservation);

					// Update batch reserved quantity
					// Note: If using same batch, ProcessDamagedBatchAsync already decreased ReservedQuantity,
					// so we need to add it back for the new reservation
					newBatch.ReservedQuantity += quantityToSwap;
					if (!usingSameBatch)
					{
						// Only call Update for different batch (damagedBatch is already updated in ProcessDamagedBatchAsync)
						_unitOfWork.Batches.Update(newBatch);
					}

					// Update stock reserved quantity
					var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
					if (stock != null)
					{
						// If using same batch: ProcessDamagedBatchAsync decreased ReservedQuantity, now increase it back
						// If using different batch: ProcessDamagedBatchAsync decreased, now increase for new batch
						stock.ReservedQuantity += quantityToSwap;
						_unitOfWork.Stocks.Update(stock);
					}

					// Get location info for response
					var newBatchWithIncludes = usingSameBatch
						? damagedBatch
						: await _unitOfWork.Batches.GetBatchByIdWithIncludesAsync(newBatch.Id);

					var response = new SwapDamagedStockResponse
					{
						NewReservationId = newReservation.Id,
						NewBatchId = newBatch.Id,
						NewBatchCode = newBatch.BatchCode,
						NewLocation = newBatchWithIncludes?.ImportDetail?.Note,
						ReservedQuantity = quantityToSwap,
						ExpiryDate = newBatch.ExpiryDate,
						Message = usingSameBatch
						? $"Successfully reserved replacement stock from same batch {newBatch.BatchCode}."
						: $"Successfully swapped damaged batch {damagedBatch.BatchCode} with new batch {newBatch.BatchCode}."
					};

					return BaseResponse<SwapDamagedStockResponse>.Ok(response);
				});
			}
			catch (Exception ex)
			{
				return BaseResponse<SwapDamagedStockResponse>.Fail(
					$"An error occurred while swapping damaged stock: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		private async Task<BaseResponse<(Order, StockReservation, Batch)?>> ValidateSwapRequestAsync(
			Guid orderId,
			SwapDamagedStockRequest request)
		{
			var order = await _unitOfWork.Orders.GetByConditionAsync(
				o => o.Id == orderId,
				o => o.Include(o => o.OrderDetails));

			if (order == null)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail("Order not found.", ResponseErrorType.NotFound);
			}

			if (order.Status != OrderStatus.Processing)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail(
					$"Order must be in Processing status to swap damaged stock. Current status: {order.Status}",
					ResponseErrorType.BadRequest);
			}

			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			var damagedReservation = reservations.FirstOrDefault(r => r.Id == request.DamagedReservationId);

			if (damagedReservation == null)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail(
					"Damaged reservation not found.",
					ResponseErrorType.NotFound);
			}

			if (damagedReservation.Status != ReservationStatus.Reserved)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail(
					"Reservation is not in Reserved status.",
					ResponseErrorType.BadRequest);
			}

			var orderDetail = order.OrderDetails.FirstOrDefault(od => od.Id == request.OrderDetailId);
			if (orderDetail == null)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail(
					"Order detail not found.",
					ResponseErrorType.NotFound);
			}

			// Use the Batch already loaded via reservation navigation property to avoid EF tracking conflicts
			var damagedBatch = damagedReservation.Batch;
			if (damagedBatch == null)
			{
				return BaseResponse<(Order, StockReservation, Batch)?>.Fail(
					"Damaged batch not found.",
					ResponseErrorType.NotFound);
			}

			return BaseResponse<(Order, StockReservation, Batch)?>.Ok((order, damagedReservation, damagedBatch));
		}

		private async Task CreateDamageAdjustmentAsync(
			Guid staffId,
			Guid orderId,
			Guid variantId,
			Guid batchId,
			int quantity,
			string? damageNote)
		{
			var stockAdjustment = new StockAdjustment
			{
				CreatedById = staffId,
				AdjustmentDate = DateTime.UtcNow,
				Reason = StockAdjustmentReason.Damage,
				Note = damageNote ?? $"Damaged during order picking for Order {orderId}",
				Status = StockAdjustmentStatus.Completed,
				AdjustmentDetails =
				[
					new StockAdjustmentDetail
					{
						ProductVariantId = variantId,
						BatchId = batchId,
						AdjustmentQuantity = -quantity,
						ApprovedQuantity = -quantity,
						Note = damageNote
					}
				]
			};

			await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
		}

		private async Task ProcessDamagedBatchAsync(
			Batch damagedBatch,
			StockReservation damagedReservation,
			int quantityToSwap,
			Guid variantId)
		{
			// Decrease damaged batch quantities
			damagedBatch.RemainingQuantity -= quantityToSwap;
			damagedBatch.ReservedQuantity -= quantityToSwap;
			_unitOfWork.Batches.Update(damagedBatch);

			// Release the damaged reservation
			damagedReservation.Status = ReservationStatus.Released;
			_unitOfWork.StockReservations.Update(damagedReservation);

			// Update stock quantities
			var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock != null)
			{
				stock.ReservedQuantity -= quantityToSwap;
				stock.TotalQuantity -= quantityToSwap;
				_unitOfWork.Stocks.Update(stock);
			}
		}

		private async Task<BaseResponse<(StockReservation, Batch)?>> ReserveNewBatchAsync(
			Guid orderId,
			Guid variantId,
			int quantityToSwap,
			Guid excludeBatchId,
			DateTime? expiresAt)
		{
			var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantAsync(variantId);

			var newBatch = availableBatches
				.Where(b => b.Id != excludeBatchId && b.AvailableInBatch >= quantityToSwap)
				.OrderBy(b => b.ExpiryDate) // FEFO: First Expiry First Out
				.FirstOrDefault();

			if (newBatch == null)
			{
				return BaseResponse<(StockReservation, Batch)?>.Fail(
					$"No available batch found with sufficient quantity ({quantityToSwap}) for variant {variantId}.",
					ResponseErrorType.BadRequest);
			}

			// Create new reservation
			var newReservation = new StockReservation
			{
				OrderId = orderId,
				BatchId = newBatch.Id,
				VariantId = variantId,
				ReservedQuantity = quantityToSwap,
				Status = ReservationStatus.Reserved,
				ExpiresAt = expiresAt
			};

			await _unitOfWork.StockReservations.AddAsync(newReservation);

			// Update batch reserved quantity
			newBatch.ReservedQuantity += quantityToSwap;
			_unitOfWork.Batches.Update(newBatch);

			// Update stock reserved quantity
			var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock != null)
			{
				stock.ReservedQuantity += quantityToSwap;
				_unitOfWork.Stocks.Update(stock);
			}

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<(StockReservation, Batch)?>.Ok((newReservation, newBatch));
		}

		#endregion
	}
}
