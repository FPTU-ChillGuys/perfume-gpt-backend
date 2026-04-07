using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.OrderHelpers;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using System.Text.Json;
using System.Text.Json.Nodes;
using static PerfumeGPT.Domain.Entities.StockAdjustmentDetail;

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
		public async Task<PickListResponse> GetPickListAsync(Order order)
		{
			if (order.Type != OrderType.Online)
				throw AppException.BadRequest("Pick list is only available for online orders.");

			var reservations = await _unitOfWork.StockReservations.GetByOrderIdAsync(order.Id);
			if (reservations.Any(r => r.Status != ReservationStatus.Reserved))
				throw AppException.BadRequest("Pick list can only be generated for orders with active reservations.");

			var pickListItems = await BuildPickListItemsAsync(order.OrderDetails, reservations);

			return new PickListResponse { OrderId = order.Id, Code = order.Code, Items = pickListItems };
		}

		private async Task<List<PickListItemResponse>> BuildPickListItemsAsync(ICollection<OrderDetail> orderDetails, IEnumerable<StockReservation> reservations)
		{
			var pickListItems = new List<PickListItemResponse>();

			var activeReservations = reservations.Where(r => r.Status == ReservationStatus.Reserved).ToList();

			var reservationsByBatch = activeReservations.ToLookup(r => r.BatchId);

			var batchIds = activeReservations.Select(r => r.BatchId).Distinct().ToList();

			var batches = await _unitOfWork.Batches.GetBatchesByIds(batchIds);
			var batchDictionary = batches.ToDictionary(b => b.Id);

			foreach (var orderDetail in orderDetails)
			{
				var batchInfoList = new List<PickListBatchInfo>();

				Guid? snapshotBatchId = null;
				try
				{
					using var jsonDoc = JsonDocument.Parse(orderDetail.Snapshot);
					if (jsonDoc.RootElement.TryGetProperty("BatchId", out var batchIdElement))
					{
						snapshotBatchId = batchIdElement.GetGuid();
					}
				}
				catch
				{
					// If snapshot parsing fails, we can choose to log this incident or ignore it based on requirements. For now, we'll ignore and proceed without batch info.
				}

				if (snapshotBatchId.HasValue)
				{
					var matchingReservations = reservationsByBatch[snapshotBatchId.Value];

					foreach (var reservation in matchingReservations)
					{
						if (batchDictionary.TryGetValue(reservation.BatchId, out var batch))
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
		#endregion Pick List Generation


		#region Order Fulfillment
		public async Task<string> FulfillOrderAsync(Guid orderId, Guid staffId, FulfillOrderRequest request)
		{
			Order? orderForGhn = null;
			ContactAddress? contactAddressForGhn = null;

			await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var order = await ValidateOrderForFulfillmentAsync(orderId);

				var batchValidation = await ValidateScannedBatchCodesAsync(order, request);
				if (!batchValidation.Success)
					throw AppException.BadRequest(batchValidation.Message ?? "Batch validation failed.");

				await _stockReservationService.CommitReservationAsync(order.Id);

				order.SetStaff(staffId);
				order.SetStatus(OrderStatus.ReadyToPick);
				_unitOfWork.Orders.Update(order);

				orderForGhn = order;
				contactAddressForGhn = order.ContactAddress;

				return true;
			});

			if (orderForGhn?.ForwardShipping != null && contactAddressForGhn != null)
			{
				var ghnOrderResult = await _shippingHelper.CreateGHNShippingOrderAsync(orderForGhn, contactAddressForGhn);
				if (!ghnOrderResult)
				{
					return $"Order stock committed locally, BUT failed to create GHN order. Please check contact address and retry GHN sync manually.";
				}

				return "Order fulfilled successfully. Stock committed and GHN shipping order created.";
			}

			return "Order fulfilled successfully. Stock committed.";
		}

		private async Task<Order> ValidateOrderForFulfillmentAsync(Guid orderId)
		{
			var order = await _unitOfWork.Orders.GetOrderForFulfillmentAsync(orderId)
			 ?? throw AppException.NotFound("Order not found.");

			if (order.Status != OrderStatus.Preparing)
				throw AppException.BadRequest($"Order must be in Preparing status. Current: {order.Status}");

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

			var batchIds = activeReservations.Select(r => r.BatchId).Distinct().ToList();
			var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id), asNoTracking: true);
			var batchDictionary = batches.ToDictionary(b => b.Id);

			// Validate all order details are included in the request
			var requestOrderDetailIds = request.Items.Select(i => i.OrderDetailId).ToHashSet();
			var orderDetailIds = order.OrderDetails.Select(od => od.Id).ToHashSet();

			if (!orderDetailIds.SetEquals(requestOrderDetailIds))
			{
				var missingDetails = orderDetailIds.Except(requestOrderDetailIds).ToList();
				var extraDetails = requestOrderDetailIds.Except(orderDetailIds).ToList();

				if (missingDetails.Count > 0)
					throw AppException.BadRequest($"Missing fulfillment items for order details: {string.Join(", ", missingDetails)}.");

				if (extraDetails.Count > 0)
					throw AppException.BadRequest($"Unknown order detail IDs provided: {string.Join(", ", extraDetails)}.");
			}

			foreach (var item in request.Items)
			{
				var orderDetail = order.OrderDetails.FirstOrDefault(od => od.Id == item.OrderDetailId) ?? throw AppException.BadRequest($"Order detail {item.OrderDetailId} not found in order.");

				// Validate quantity matches order detail quantity
				if (item.Quantity != orderDetail.Quantity)
					throw AppException.BadRequest($"Quantity mismatch for order detail {item.OrderDetailId}. Expected: {orderDetail.Quantity}, Provided: {item.Quantity}.");

				Guid? expectedBatchId = null;
				try
				{
					using var jsonDoc = JsonDocument.Parse(orderDetail.Snapshot);
					if (jsonDoc.RootElement.TryGetProperty("BatchId", out var batchIdElement))
					{
						expectedBatchId = batchIdElement.GetGuid();
					}
				}
				catch { }

				if (expectedBatchId.HasValue)
				{
					if (!batchDictionary.TryGetValue(expectedBatchId.Value, out var expectedBatch))
						throw AppException.Internal($"Database inconsistency: Expected batch with ID {expectedBatchId.Value} not found.");

					if (expectedBatch.BatchCode != item.ScannedBatchCode)
						throw AppException.BadRequest($"Scanned batch code '{item.ScannedBatchCode}' is incorrect for order detail {item.OrderDetailId}. Expected: '{expectedBatch.BatchCode}'.");

					var exactReservation = activeReservations.FirstOrDefault(r => r.BatchId == expectedBatchId.Value && r.VariantId == orderDetail.VariantId);
					if (exactReservation == null || exactReservation.ReservedQuantity < orderDetail.Quantity)
						throw AppException.BadRequest($"Reservation mismatch or insufficient reserved quantity for batch '{expectedBatch.BatchCode}' and order detail {item.OrderDetailId}.");
				}
				else
				{
					var variantReservations = activeReservations.Where(r => r.VariantId == orderDetail.VariantId).ToList();

					var validBatchCodes = variantReservations
						.Select(r => batchDictionary.TryGetValue(r.BatchId, out var b) ? b.BatchCode : null)
						.Where(code => code != null)
						.ToList();

					if (!validBatchCodes.Contains(item.ScannedBatchCode))
						throw AppException.BadRequest($"Scanned batch code '{item.ScannedBatchCode}' does not match any reserved batch for order detail {item.OrderDetailId}. Valid codes: {string.Join(", ", validBatchCodes)}.");
				}
			}

			return BaseResponse<bool>.Ok(true);
		}

		#endregion Order Fulfillment

		#region Damaged Stock Handling


		public async Task<SwapDamagedStockResponse> SwapDamagedStockAsync(Guid orderId, Guid staffId, SwapDamagedStockRequest request)
		{
			if (request.DamagedQuantity <= 0)
				throw AppException.BadRequest("Damaged quantity must be greater than 0.");

			var (order, damagedReservation, damagedBatch) = await ValidateSwapRequestAsync(orderId, request);

			if (request.DamagedQuantity > damagedReservation.ReservedQuantity)
				throw AppException.BadRequest($"Damaged quantity ({request.DamagedQuantity}) cannot exceed reserved quantity ({damagedReservation.ReservedQuantity}).");

			var quantityToSwap = request.DamagedQuantity;
			var variantId = damagedReservation.VariantId;

			return await _unitOfWork.ExecuteInTransactionAsync(async () =>
			{
				var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
					?? throw AppException.NotFound("Stock not found.");

				StockReservation? primaryNewReservation = null;
				Batch? primaryReplacementBatch = null;
				int primaryReservedQuantity = 0;

				// Step 1: Xuất kho phế phẩm (Ghi nhận hỏng hóc)
				await CreateDamageAdjustmentAsync(staffId, orderId, variantId, damagedBatch.Id, quantityToSwap, request.DamageNote);

				// Step 2: Xử lý Lô cũ VÀ Phiếu giữ kho cũ
				damagedBatch.Release(quantityToSwap);
				damagedBatch.DecreaseQuantity(quantityToSwap);
				_unitOfWork.Batches.Update(damagedBatch);

				if (quantityToSwap == damagedReservation.ReservedQuantity)
				{
					// Hỏng toàn bộ -> Huỷ phiếu giữ kho cũ
					damagedReservation.Release();
				}
				else
				{
					// 💥 Hỏng 1 phần -> Giảm số lượng giữ kho cũ (Cần tạo hàm DecreaseQuantity bên trong Entity StockReservation)
					damagedReservation.DecreaseQuantity(quantityToSwap);
				}
				_unitOfWork.StockReservations.Update(damagedReservation);

				stock.ReleaseReservation(quantityToSwap);
				stock.Decrease(quantityToSwap);

				// Step 3: Tìm các lô mới thay thế (Áp dụng FIFO thay vì FirstOrDefault để gom đủ hàng)
				var availableBatches = await _unitOfWork.Batches.GetAvailableBatchesByVariantIdAsync(variantId);
				var remainingToSwap = quantityToSwap;
				var replacementAllocations = new List<(Batch Batch, int Quantity)>();

				foreach (var batch in availableBatches.Where(b => b.Id != damagedBatch.Id).OrderBy(b => b.ExpiryDate))
				{
					if (remainingToSwap <= 0) break;
					if (batch.AvailableInBatch <= 0) continue;

					var take = Math.Min(remainingToSwap, batch.AvailableInBatch);
					replacementAllocations.Add((batch, take));
					remainingToSwap -= take;
				}

				if (remainingToSwap > 0)
					throw AppException.BadRequest($"Not enough stock across all batches to replace damaged items. Missing: {remainingToSwap}");

				// Step 4: Kỹ thuật Tách (Splitting) OrderDetail
				var targetOrderDetail = order.OrderDetails.FirstOrDefault(od =>
				{
					try
					{
						using var doc = JsonDocument.Parse(od.Snapshot);
						return doc.RootElement.TryGetProperty("BatchId", out var bIdElement) && bIdElement.GetGuid() == damagedBatch.Id;
					}
					catch { return false; }
				}) ?? throw AppException.Internal("Database inconsistency: Target OrderDetail not found for the damaged batch.");

				// Tính toán đơn giá và chiết khấu chia đều trên 1 sản phẩm
				var unitPrice = targetOrderDetail.UnitPrice;
				// Giả định bạn có thuộc tính ApportionedDiscount trong OrderDetail
				var perItemDiscount = targetOrderDetail.Quantity > 0 ? (targetOrderDetail.ApportionedDiscount / targetOrderDetail.Quantity) : 0;

				if (quantityToSwap < targetOrderDetail.Quantity || replacementAllocations.Count > 1)
				{
					// KỊCH BẢN PHỨC TẠP: Hỏng 1 phần HOẶC phải gom từ nhiều lô mới
					var goodQuantity = targetOrderDetail.Quantity - quantityToSwap;

					if (goodQuantity > 0)
					{
						// Giữ lại số chai còn xài được ở OrderDetail cũ (Cần tạo hàm UpdateQuantityAndDiscount)
						targetOrderDetail.UpdateQuantityAndDiscount(goodQuantity, perItemDiscount * goodQuantity);
					}
					else
					{
						order.OrderDetails.Remove(targetOrderDetail); // Tuỳ cấu hình EF Core của bạn có cho phép xoá ko
					}

					// Tạo các dòng OrderDetail mới cho phần hàng thay thế
					foreach (var (repBatch, repQty) in replacementAllocations)
					{
						// Tạo Reservation mới
						var newReservation = new StockReservation(orderId, repBatch.Id, variantId, repQty, damagedReservation.ExpiresAt);
						await _unitOfWork.StockReservations.AddAsync(newReservation);
						primaryNewReservation ??= newReservation;
						if (primaryReplacementBatch is null)
						{
							primaryReplacementBatch = repBatch;
							primaryReservedQuantity = repQty;
						}
						repBatch.Reserve(repQty);
						_unitOfWork.Batches.Update(repBatch);
						stock.Reserve(repQty);

						// Thêm OrderDetail mới bằng cách clone Snapshot cũ
						var snapshotNode = JsonNode.Parse(targetOrderDetail.Snapshot)!.AsObject();
						snapshotNode["BatchId"] = repBatch.Id;
						snapshotNode["BatchCode"] = repBatch.BatchCode;
						snapshotNode["ExpiryDate"] = repBatch.ExpiryDate;

						var newDetail = OrderDetail.Create(variantId, repQty, unitPrice, snapshotNode.ToJsonString());
						newDetail.ApplyDiscount(perItemDiscount * repQty); // Copy tỷ lệ giảm giá sang
						order.AddOrderDetails([newDetail]);
					}
				}
				else
				{
					// KỊCH BẢN ĐƠN GIẢN: Hỏng 100% lô cũ và tìm được 1 lô mới bù đúng 100%
					var repBatch = replacementAllocations[0].Batch;
					var repQty = replacementAllocations[0].Quantity;

					var newReservation = new StockReservation(orderId, repBatch.Id, variantId, repQty, damagedReservation.ExpiresAt);
					await _unitOfWork.StockReservations.AddAsync(newReservation);
					primaryNewReservation = newReservation;
					primaryReplacementBatch = repBatch;
					primaryReservedQuantity = repQty;

					repBatch.Reserve(repQty);
					_unitOfWork.Batches.Update(repBatch);
					stock.Reserve(repQty);

					targetOrderDetail.UpdateBatchInfoInSnapshot(repBatch.Id, repBatch.BatchCode, repBatch.ExpiryDate);
				}

				_unitOfWork.Stocks.Update(stock);

				if (primaryNewReservation is null || primaryReplacementBatch is null)
					throw AppException.Internal("Database inconsistency: Replacement reservation was not created.");

				return new SwapDamagedStockResponse
				{
					NewReservationId = primaryNewReservation.Id,
					NewBatchId = primaryReplacementBatch.Id,
					NewBatchCode = primaryReplacementBatch.BatchCode,
					NewLocation = primaryReplacementBatch.ImportDetail?.Note,
					ReservedQuantity = primaryReservedQuantity,
					ExpiryDate = primaryReplacementBatch.ExpiryDate,
					Message = $"Successfully swapped {quantityToSwap} damaged items. System used {replacementAllocations.Count} replacement batch(es)."
				};
			});
		}

		private async Task<(Order, StockReservation, Batch)> ValidateSwapRequestAsync(Guid orderId, SwapDamagedStockRequest request)
		{
			var order = await _unitOfWork.Orders.GetOrderForSwapDamagedStockAsync(orderId)
				 ?? throw AppException.NotFound("Order not found.");

			if (order.Status != OrderStatus.Preparing)
				throw AppException.BadRequest($"Order must be in Preparing status.");

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

			var detailPayload = new StockAdjustmentDetailPayload
			{
				ProductVariantId = variantId,
				BatchId = batchId,
				AdjustmentQuantity = -quantity,
				Note = damageNote
			};
			stockAdjustment.AddApprovedDetail(detailPayload, -quantity);

			stockAdjustment.UpdateStatus(StockAdjustmentStatus.InProgress);
			stockAdjustment.Complete(staffId);

			await _unitOfWork.StockAdjustments.AddAsync(stockAdjustment);
		}
		#endregion Damaged Stock Handling
	}
}
