using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services.Helpers.OrderHelpers
{
	public class OrderFulfillmentService : IOrderFulfillmentService
	{
		#region Dependencies
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
		#endregion Dependencies


		#region Pick List Generation
		public async Task<PickListResponse> GetPickListAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetPaidOrderForPickListAsync(orderId)
				 ?? throw AppException.NotFound("Paid order not found.");

			if (order.Type != OrderType.Online)
				throw AppException.BadRequest("Pick list is only available for online orders.");

			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			if (reservations.Any(r => r.Status != ReservationStatus.Reserved))
				throw AppException.BadRequest("Pick list can only be generated for orders with active reservations.");

			var pickListItems = await BuildPickListItemsAsync(order.OrderDetails, reservations);

			return new PickListResponse { OrderId = order.Id, Items = pickListItems };
		}

		private async Task<List<PickListItemResponse>> BuildPickListItemsAsync(ICollection<OrderDetail> orderDetails, IEnumerable<StockReservation> reservations)
		{
			var pickListItems = new List<PickListItemResponse>();

			var reservationsByVariant = reservations
				.Where(r => r.Status == ReservationStatus.Reserved)
				.GroupBy(r => r.VariantId).ToList();

			foreach (var orderDetail in orderDetails)
			{
				var variantReservations = reservationsByVariant.FirstOrDefault(g => g.Key == orderDetail.VariantId);

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
								Note = batch.ImportDetail?.Note,
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
		public async Task<string> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			Order? orderForGhn = null;
			RecipientInfo? recipientForGhn = null;

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await ValidateOrderForFulfillmentAsync(orderId);

				var batchValidation = await ValidateScannedBatchCodesAsync(order, request);
				if (!batchValidation.Success)
					throw AppException.BadRequest(batchValidation.Message ?? "Batch validation failed.");

				var recipientInfo = await _unitOfWork.RecipientInfos.GetByOrderIdAsync(order.Id);

				await _stockReservationService.CommitReservationAsync(order.Id);

				order.SetStatus(OrderStatus.Delivering);
				order.SetStaff(staffId);
				_unitOfWork.Orders.Update(order);

				MarkShippingAsDelivering(order);

				orderForGhn = order;
				recipientForGhn = recipientInfo;

				return true;
			});

			if (orderForGhn?.ShippingInfo != null && recipientForGhn != null)
			{
				var ghnOrderResult = await _shippingHelper.CreateGHNShippingOrderAsync(orderForGhn, recipientForGhn);
				if (!ghnOrderResult.Success)
				{
					return $"Order stock committed locally, BUT failed to create GHN order: {ghnOrderResult.Message}. Please check recipient info and retry GHN sync manually.";
				}

				await _unitOfWork.ExecuteInTransactionAsync(async () =>
				{
					return true;
				});
			}

			return "Order fulfilled successfully. Stock committed and GHN shipping order created.";
		}

		private async Task<Order> ValidateOrderForFulfillmentAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderForFulfillmentAsync(orderId)
			 ?? throw AppException.NotFound("Order not found.");

			if (order.Status != OrderStatus.Processing)
				throw AppException.BadRequest($"Order must be in Processing status. Current: {order.Status}");

			if (order.Type != OrderType.Online)
				throw AppException.BadRequest("Only online orders can be fulfilled through this method.");

			return order;
		}

		private async Task<BaseResponse<bool>> ValidateScannedBatchCodesAsync(Order order, FulfillOrderRequest request)
		{
			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			var activeReservations = reservations.Where(r => r.Status == ReservationStatus.Reserved).ToList();

			if (activeReservations.Count == 0)
				throw AppException.BadRequest("No active reservations found.");

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

		private void MarkShippingAsDelivering(Order order)
		{
			if (order.ShippingInfo != null)
			{
				order.ShippingInfo.MarkAsDelivering();
				_unitOfWork.ShippingInfos.Update(order.ShippingInfo);
			}
		}

		#endregion

		#region Damaged Stock Handling
		public async Task<SwapDamagedStockResponse> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request)
		{
			var (order, damagedReservation, damagedBatch) = await ValidateSwapRequestAsync(orderId, request);
			var quantityToSwap = damagedReservation.ReservedQuantity;
			var variantId = damagedReservation.VariantId;

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			  {
				  var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
				  ?? throw AppException.NotFound("Stock not found.");

				  // Step 1: Create stock adjustment for damaged batch
				  await CreateDamageAdjustmentAsync(staffId, orderId, variantId, damagedBatch.Id, quantityToSwap, request.DamageNote);

				  // Step 2: Process damaged batch and release reservation
				  await ProcessDamagedBatchAsync(damagedBatch, damagedReservation, quantityToSwap, variantId);
				  damagedReservation.Release();
				  damagedBatch.Release(quantityToSwap);
				  damagedBatch.DecreaseQuantity(quantityToSwap);

				  // Step 3: Stock update for damaged batch
				  stock.ReleaseReservation(quantityToSwap);
				  stock.Decrease(quantityToSwap);

				  _unitOfWork.StockReservations.Update(damagedReservation);
				  _unitOfWork.Batches.Update(damagedBatch);

				  // Step 4: Reserve new batch for the order
				  var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);
				  var newBatch = availableBatches
					  .Where(b => b.Id != damagedBatch.Id && b.AvailableInBatch >= quantityToSwap)
					  .OrderBy(b => b.ExpiryDate)
					  .FirstOrDefault() ?? throw AppException.BadRequest($"No available batch found for replacement.");

				  // Step 5: Create new reservation for the order
				  var newReservation = new StockReservation(orderId, newBatch.Id, variantId, quantityToSwap, damagedReservation.ExpiresAt);
				  await _unitOfWork.StockReservations.AddAsync(newReservation);

				  newBatch.Reserve(quantityToSwap);
				  _unitOfWork.Batches.Update(newBatch);

				  // Step 6: Update stock reserved quantity
				  stock.Reserve(quantityToSwap);
				  _unitOfWork.Stocks.Update(stock);

				  var newBatchWithIncludes = await _unitOfWork.Batches.GetBatchByIdWithIncludesAsync(newBatch.Id);

				  return new SwapDamagedStockResponse
				  {
					  NewReservationId = newReservation.Id,
					  NewBatchId = newBatch.Id,
					  NewBatchCode = newBatch.BatchCode,
					  NewLocation = newBatchWithIncludes?.ImportDetail?.Note,
					  ReservedQuantity = quantityToSwap,
					  ExpiryDate = newBatch.ExpiryDate,
					  Message = $"Successfully swapped damaged batch with new batch {newBatch.BatchCode}."
				  };
			  });
		}

		private async Task<(Order, StockReservation, Batch)> ValidateSwapRequestAsync(Guid orderId, SwapDamagedStockRequest request)
		{
			var order = await _unitOfWork.Orders.GetOrderForSwapDamagedStockAsync(orderId)
				 ?? throw AppException.NotFound("Order not found.");

			if (order.Status != OrderStatus.Processing)
				throw AppException.BadRequest($"Order must be in Processing status.");

			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			var damagedReservation = reservations.FirstOrDefault(r => r.Id == request.DamagedReservationId)
				?? throw AppException.NotFound("Damaged reservation not found.");

			if (damagedReservation.Status != ReservationStatus.Reserved)
				throw AppException.BadRequest("Reservation is not in Reserved status.");

			var damagedBatch = damagedReservation.Batch
				?? throw AppException.NotFound("Damaged batch not found.");

			return (order, damagedReservation, damagedBatch);
		}

		private async Task CreateDamageAdjustmentAsync(
			Guid staffId,
			Guid orderId,
			Guid variantId,
			Guid batchId,
			int quantity,
			string? damageNote)
		{
			var stockAdjustment = StockAdjustment.Create(
				 staffId,
				 DateTime.UtcNow,
				 StockAdjustmentReason.Damage,
				 damageNote ?? $"Damaged during order picking for Order {orderId}");

			stockAdjustment.AddApprovedDetail(
				variantId,
				batchId,
				-quantity,
				-quantity,
				damageNote);

			stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
			stockAdjustment.Complete(staffId);

			await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
		}

		private async Task ProcessDamagedBatchAsync(
			Batch damagedBatch,
			StockReservation damagedReservation,
			int quantityToSwap,
			Guid variantId)
		{
			// Decrease damaged batch quantities
			damagedBatch.Release(quantityToSwap);
			damagedBatch.DecreaseQuantity(quantityToSwap);
			_unitOfWork.Batches.Update(damagedBatch);

			// Release the damaged reservation
			damagedReservation.Release();
			_unitOfWork.StockReservations.Update(damagedReservation);

			// Update stock quantities
			var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId);
			if (stock != null)
			{
				stock.ReleaseReservation(quantityToSwap);
				stock.Decrease(quantityToSwap);
				_unitOfWork.Stocks.Update(stock);
			}
		}

		#endregion
	}
}
