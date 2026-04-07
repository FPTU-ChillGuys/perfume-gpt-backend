using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class CartService : ICartService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVoucherService _voucherService;
		private readonly ISignalRService _signalRService;

		public CartService(
			IUnitOfWork unitOfWork,
         IVoucherService voucherService,
			ISignalRService signalRService)
		{
			_unitOfWork = unitOfWork;
			_voucherService = voucherService;
           _signalRService = signalRService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<PreviewPosOrderResponse>> PreviewPosOrderAsync(PreviewPosOrderRequest request)
		{
			if (request.ScannedItems == null || request.ScannedItems.Count == 0)
				throw AppException.BadRequest("No items scanned.");

			// 1. Gom nhóm và TÍNH TỔNG Quantity gửi lên
			var groupedScans = request.ScannedItems
				.GroupBy(x => new { x.Barcode, x.BatchCode })
				.Select(g => new
				{
					g.Key.Barcode,
					g.Key.BatchCode,
					Quantity = g.Sum(item => item.Quantity) // Cộng dồn số lượng
				})
				.ToList();

			var checkoutItems = new List<CartCheckoutItemDto>();

			// 2. TRUY VẤN DATABASE LẤY GIÁ VÀ KIỂM TRA LÔ
			foreach (var scan in groupedScans)
			{
				// Lấy Variant qua Barcode
				var variantResponse = await _unitOfWork.Variants.GetByBarcodeAsync(scan.Barcode)
					?? throw AppException.NotFound("Variant not found");

				var variant = variantResponse;

				// Lấy chính xác Batch qua BatchCode VÀ VariantId
				var batch = await _unitOfWork.Batches.FirstOrDefaultAsync(b =>
					b.BatchCode == scan.BatchCode && b.VariantId == variant.Id)
					?? throw AppException.NotFound($"Batch {scan.BatchCode} not found for product {variant.Sku}.");

				var subTotal = variant.BasePrice * scan.Quantity;

				checkoutItems.Add(new CartCheckoutItemDto
				{
					VariantId = variant.Id,
					BatchId = batch.Id,
					VariantName = $"{variant.Sku} - {variant.VolumeMl}ml - {variant.Type}",
					Quantity = scan.Quantity,
					UnitPrice = variant.BasePrice,
					SubTotal = subTotal,
					Discount = 0m,
					FinalTotal = subTotal,
					// Dữ liệu phụ để map trả về UI
					ImageUrl = variant.Media?.FirstOrDefault(m => m.IsPrimary)?.Url ?? string.Empty,
					BatchCode = batch.BatchCode
				});
			}

			// 3. ĐƯA VÀO CỖ MÁY TÍNH TIỀN CHUNG (Shared Pricing Engine)
			// Bạn phải tái cấu trúc hàm BuildCheckoutPricingAsync cũ để nhận đầu vào là List<CartCheckoutItemDto>
			var (pricedItems, subtotal, finalAmount, voucherMessage) = await CalculatePricingEngineAsync(
				checkoutItems,
				request.VoucherCode,
				request.CustomerId);

			// 4. MAP SANG DTO HIỂN THỊ
			var responseItems = pricedItems.Select(item => new PosOrderDetailListItem
			{
				VariantId = item.VariantId,
				BatchId = item.BatchId!.Value, // Chắc chắn có vì POS ép quét batch
				VariantName = item.VariantName,
				Quantity = item.Quantity,
				UnitPrice = item.UnitPrice,
				SubTotal = item.SubTotal,
				Discount = item.Discount,
				FinalTotal = item.FinalTotal,
				ImageUrl = item.ImageUrl ?? "",
				BatchCode = item.BatchCode ?? throw AppException.BadRequest("Batch code is required for POS items.")
			}).ToList();

			var response = new PreviewPosOrderResponse
			{
				Items = responseItems,
				SubTotal = subtotal,
				Discount = subtotal - finalAmount,
				TotalPrice = finalAmount
			};

			if (!string.IsNullOrWhiteSpace(request.SessionId))
			{
				var customerDisplayData = new CartDisplayDto
				{
					Items = response.Items,
					SubTotal = response.SubTotal,
					Discount = response.Discount,
					TotalPrice = response.TotalPrice
				};

				await _signalRService.UpdateCustomerDisplayAsync(request.SessionId, customerDisplayData);
			}

			return BaseResponse<PreviewPosOrderResponse>.Ok(response, string.IsNullOrWhiteSpace(voucherMessage) ? "Order previewed successfully." : voucherMessage);
		}

		public async Task<CartCheckoutResponse> GetCartForCheckoutAsync(Guid userId, GetCartTotalRequest request)
		{
			var (items, _, finalAmount, _) = await BuildCheckoutPricingAsync(userId, request);
			if (items.Count == 0)
			{
				return new CartCheckoutResponse
				{
					Items = [],
					ShippingFee = 0m,
					TotalPrice = 0m
				};
			}

			var response = new CartCheckoutResponse
			{
				Items = items,
				ShippingFee = 0m,
				TotalPrice = finalAmount
			};

			return response;
		}

		public async Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId, GetPagedCartItemsRequest request)
		{
			var items = await _unitOfWork.CartItems.GetCartItemsByUserIdAsync(userId, request.ItemIds);
			if (items == null || items.Count == 0)
			{
				return BaseResponse<GetCartItemsResponse>.Ok(
					new GetCartItemsResponse { Items = [] },
					"Cart is empty");
			}

			return BaseResponse<GetCartItemsResponse>.Ok(
				new GetCartItemsResponse { Items = items },
				"Cart items retrieved successfully");
		}

		public async Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, GetCartTotalRequest request)
		{
			var (items, subtotal, finalAmount, voucherMessage) = await BuildCheckoutPricingAsync(userId, request);
			if (items.Count == 0)
			{
				return BaseResponse<GetCartTotalResponse>.Ok(
					new GetCartTotalResponse
					{
						Subtotal = 0m,
						ShippingFee = 0m,
						TotalPrice = 0m
					},
					"Cart is empty");
			}

			// 4. Build response
			var response = new GetCartTotalResponse
			{
				Subtotal = subtotal,
				ShippingFee = 0,
				Discount = subtotal - finalAmount,
				TotalPrice = finalAmount + 0
			};

			return BaseResponse<GetCartTotalResponse>.Ok(
				 response,
				 string.IsNullOrWhiteSpace(voucherMessage)
					 ? "Cart total calculated successfully"
					 : voucherMessage);
		}

		public async Task<(List<CartCheckoutItemDto> Items, decimal Subtotal, decimal FinalAmount, string? Message)> CalculatePricingEngineAsync(
			List<CartCheckoutItemDto> checkoutItems, string? voucherCode, Guid? userId)
		{
			var subtotal = checkoutItems.Sum(x => x.SubTotal);

			if (string.IsNullOrWhiteSpace(voucherCode))
				return (checkoutItems, subtotal, subtotal, null);

			var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode)
				?? throw AppException.NotFound("Voucher not found");

			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
				voucherCode, userId, subtotal, null, checkoutItems.Select(x => x.VariantId));

			if (!voucherValidation)
				throw AppException.BadRequest("Voucher validation failed.");

			if (voucher.ApplyType == VoucherType.Product && voucher.CampaignId.HasValue)
			{
				var (pricedItems, message) = await ApplyProductLevelVoucherDiscountAsync(voucher, checkoutItems);
				var finalAmount = pricedItems.Sum(x => x.FinalTotal);
				return (pricedItems, subtotal, finalAmount, message);
			}

			var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherCode, subtotal);
			var totalDiscount = subtotal - discountedTotal;

			var adjustedItems = ApplyProportionalDiscount(checkoutItems, totalDiscount, x => x.SubTotal);
			var finalTotal = adjustedItems.Sum(x => x.FinalTotal);

			return (adjustedItems, subtotal, finalTotal, null);
		}

		private async Task<(List<CartCheckoutItemDto> Items, decimal Subtotal, decimal FinalAmount, string? Message)> BuildCheckoutPricingAsync(Guid userId, GetCartTotalRequest request)
		{
			var items = await _unitOfWork.CartItems.GetCartItemPricesAsync(userId, request.ItemIds);
			if (items == null || items.Count == 0)
				return ([], 0m, 0m, null);

			var checkoutItems = items
				.Select(item => new CartCheckoutItemDto
				{
					VariantId = item.VariantId,
					VariantName = item.VariantName,
					Quantity = item.Quantity,
					UnitPrice = item.VariantPrice,
					SubTotal = item.SubTotal,
					Discount = 0m,
					FinalTotal = item.SubTotal
				})
				.ToList();

			var subtotal = checkoutItems.Sum(x => x.SubTotal);
			if (string.IsNullOrWhiteSpace(request.VoucherCode))
				return (checkoutItems, subtotal, subtotal, null);

			var voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode)
				?? throw AppException.NotFound("Voucher not found");

			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
				request.VoucherCode,
				userId,
				subtotal,
				null,
				items.Select(x => x.VariantId));

			if (!voucherValidation)
				throw AppException.BadRequest("Voucher validation failed");

			if (voucher.ApplyType == VoucherType.Product && voucher.CampaignId.HasValue)
			{
				var (pricedItems, message) = await ApplyProductLevelVoucherDiscountAsync(voucher, checkoutItems);
				var finalAmount = pricedItems.Sum(x => x.FinalTotal);
				return (pricedItems, subtotal, finalAmount, message);
			}

			var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(request.VoucherCode, subtotal);
			var totalDiscount = subtotal - discountedTotal;
			var adjustedItems = ApplyProportionalDiscount(checkoutItems, totalDiscount, x => x.SubTotal);
			var finalTotal = adjustedItems.Sum(x => x.FinalTotal);

			return (adjustedItems, subtotal, finalTotal, null);
		}

		private async Task<(List<CartCheckoutItemDto> Items, string? Message)> ApplyProductLevelVoucherDiscountAsync(VoucherResponse voucher, List<CartCheckoutItemDto> items)
		{
			if (!voucher.CampaignId.HasValue)
			{
				var subtotal = items.Sum(x => x.SubTotal);
				var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucher.Code, subtotal);
				var totalDiscount = subtotal - discountedTotal;
				return (ApplyProportionalDiscount(items, totalDiscount, x => x.SubTotal), null);
			}

			var variantIds = items.Select(x => x.VariantId).Distinct().ToList();
			var promoItemsByVariant = await GetActivePromotionsByVariantAsync(voucher.CampaignId.Value, voucher.TargetItemType, variantIds);

			if (promoItemsByVariant.Count == 0)
				return (items, "No eligible product is in active promotion for this voucher");

			var batchAvailability = await GetBatchAvailabilityAsync(promoItemsByVariant.SelectMany(x => x.Value));
			var (allocations, eligibleSubtotal, messageLines) = EvaluateEligibleStock(items, promoItemsByVariant, batchAvailability);
			if (eligibleSubtotal <= 0)
				return (allocations.Select(x => x.Item).ToList(), "No quantity is available in promotion batch for voucher discount");

			var discountAmount = CalculateDiscountAmount(voucher, eligibleSubtotal);
			var discountedItems = ApplyProportionalDiscount(allocations, discountAmount);

			var message = messageLines.Count > 0
				? string.Join(" | ", messageLines)
				: null;

			return (discountedItems, message);
		}

		private static List<CartCheckoutItemDto> ApplyProportionalDiscount(
			List<CartCheckoutItemDto> items,
			decimal totalDiscount,
			Func<CartCheckoutItemDto, decimal> weightSelector)
		{
			if (totalDiscount <= 0)
				return items;

			var weightedItems = items
				.Select((item, index) => new
				{
					Item = item,
					Index = index,
					Weight = weightSelector(item)
				})
				.Where(x => x.Weight > 0)
				.ToList();

			if (weightedItems.Count == 0)
				return items;

			var weightTotal = weightedItems.Sum(x => x.Weight);
			if (weightTotal <= 0)
				return items;

			var result = items.ToList();
			decimal allocated = 0m;

			for (var i = 0; i < weightedItems.Count; i++)
			{
				var target = weightedItems[i];
				var isLast = i == weightedItems.Count - 1;
				var rawDiscount = isLast
					? totalDiscount - allocated
					: Math.Round((target.Weight / weightTotal) * totalDiscount, 2, MidpointRounding.AwayFromZero);

				var safeDiscount = Math.Max(0m, Math.Min(rawDiscount, target.Item.SubTotal));
				allocated += safeDiscount;

				result[target.Index] = target.Item with
				{
					Discount = safeDiscount,
					FinalTotal = target.Item.SubTotal - safeDiscount
				};
			}

			return result;
		}

		private static List<CartCheckoutItemDto> ApplyProportionalDiscount(
			List<CheckoutLineAllocation> allocations,
			decimal totalDiscount)
		{
			var result = allocations.Select(x => x.Item).ToList();
			if (totalDiscount <= 0)
				return result;

			var weightedItems = allocations
				.Select((allocation, index) => new
				{
					allocation.Item,
					allocation.IsEligible,
					Index = index
				})
				.Where(x => x.IsEligible && x.Item.SubTotal > 0)
				.ToList();

			if (weightedItems.Count == 0)
				return result;

			var weightTotal = weightedItems.Sum(x => x.Item.SubTotal);
			if (weightTotal <= 0)
				return result;

			decimal allocated = 0m;
			for (var i = 0; i < weightedItems.Count; i++)
			{
				var target = weightedItems[i];
				var isLast = i == weightedItems.Count - 1;
				var rawDiscount = isLast
					? totalDiscount - allocated
					: Math.Round((target.Item.SubTotal / weightTotal) * totalDiscount, 2, MidpointRounding.AwayFromZero);

				var safeDiscount = Math.Max(0m, Math.Min(rawDiscount, target.Item.SubTotal));
				allocated += safeDiscount;

				result[target.Index] = target.Item with
				{
					Discount = safeDiscount,
					FinalTotal = target.Item.SubTotal - safeDiscount
				};
			}

			return result;
		}

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId, List<Guid>? itemIds)
		{
			var hasItems = await _unitOfWork.CartItems.HasItemsAsync(userId);
			if (hasItems)
			{
				await _unitOfWork.CartItems.ClearCartByUserIdAsync(userId, itemIds);
			}
			return BaseResponse<string>.Ok("Cart cleared successfully");
		}


		private static (List<CheckoutLineAllocation> Allocations, decimal EligibleSubtotal, List<string> MessageLines)
		   EvaluateEligibleStock(
			   List<CartCheckoutItemDto> items,
			   Dictionary<Guid, List<PromotionItem>> promoItemsByVariant,
			   Dictionary<Guid, int> batchAvailability)
		{
			var allocations = new List<CheckoutLineAllocation>();
			var eligibleSubtotal = 0m;
			var messageLines = new List<string>();

			foreach (var line in items)
			{
				if (!promoItemsByVariant.TryGetValue(line.VariantId, out var variantPromotions))
				{
					allocations.Add(new CheckoutLineAllocation(CreateSplitItem(line, line.Quantity, batchId: null), false));
					messageLines.Add($"'{line.VariantName}': {line.Quantity} not in promotion.");
					continue;
				}

				if (line.BatchId.HasValue)
				{
					var hasGlobalPromotion = variantPromotions.Any(x => !x.BatchId.HasValue);
					var specificPromo = variantPromotions.FirstOrDefault(x => x.BatchId == line.BatchId.Value);

					if (hasGlobalPromotion)
					{
						allocations.Add(new CheckoutLineAllocation(line, true));
						eligibleSubtotal += line.SubTotal;
					}
					else if (specificPromo != null)
					{
						var allowedQty = 0;
						if (batchAvailability.TryGetValue(line.BatchId.Value, out var available) && available > 0)
						{
							allowedQty = Math.Min(line.Quantity, available);
							batchAvailability[line.BatchId.Value] = available - allowedQty;
						}

						if (allowedQty > 0)
						{
							var eligibleLine = line with { Quantity = allowedQty, SubTotal = line.UnitPrice * allowedQty, FinalTotal = line.UnitPrice * allowedQty };
							allocations.Add(new CheckoutLineAllocation(eligibleLine, true));
							eligibleSubtotal += eligibleLine.SubTotal;
						}

						var excludedQty = line.Quantity - allowedQty;
						if (excludedQty > 0)
						{
							var ineligibleLine = line with { Quantity = excludedQty, SubTotal = line.UnitPrice * excludedQty, FinalTotal = line.UnitPrice * excludedQty };
							allocations.Add(new CheckoutLineAllocation(ineligibleLine, false));
							messageLines.Add($"'{line.VariantName}' (Batch {line.BatchCode}): only {allowedQty} eligible for discount, {excludedQty} over promo limit.");
						}
					}
					else
					{
						allocations.Add(new CheckoutLineAllocation(line, false));
					}

					continue;
				}

				var (lineAllocations, allowedQtyOnline) = SplitLineByBatch(line, variantPromotions, batchAvailability);
				allocations.AddRange(lineAllocations);
				eligibleSubtotal += lineAllocations
					.Where(x => x.IsEligible)
					.Sum(x => x.Item.SubTotal);

				var excludedQtyOnline = line.Quantity - allowedQtyOnline;

				if (excludedQtyOnline > 0)
					messageLines.Add($"'{line.VariantName}': only {allowedQtyOnline} in promotion, {excludedQtyOnline} not.");
			}

			return (allocations, eligibleSubtotal, messageLines);
		}

		private static CartCheckoutItemDto CreateSplitItem(CartCheckoutItemDto source, int quantity, Guid? batchId)
		{
			var subTotal = source.UnitPrice * quantity;
			return source with
			{
				BatchId = batchId,
				Quantity = quantity,
				SubTotal = subTotal,
				Discount = 0m,
				FinalTotal = subTotal
			};
		}

		private static (List<CheckoutLineAllocation> Allocations, int AllowedQty) SplitLineByBatch(
			CartCheckoutItemDto line,
			List<PromotionItem> variantPromotions,
			Dictionary<Guid, int> batchAvailability)
		{
			var allocations = new List<CheckoutLineAllocation>();

			var hasGlobalPromotion = variantPromotions.Any(x => !x.BatchId.HasValue);
			if (hasGlobalPromotion)
			{
				allocations.Add(new CheckoutLineAllocation(CreateSplitItem(line, line.Quantity, batchId: null), true));
				return (allocations, line.Quantity);
			}

			var allowedQty = 0;
			var remainingNeed = line.Quantity;
			var promotionBatchIds = variantPromotions
				.Where(x => x.BatchId.HasValue)
				.Select(x => x.BatchId!.Value)
				.Distinct()
				.ToList();

			foreach (var promotionBatchId in promotionBatchIds)
			{
				if (remainingNeed <= 0) break;

				if (batchAvailability.TryGetValue(promotionBatchId, out var available) && available > 0)
				{
					var useQty = Math.Min(remainingNeed, available);
					allocations.Add(new CheckoutLineAllocation(CreateSplitItem(line, useQty, promotionBatchId), true));
					allowedQty += useQty;
					remainingNeed -= useQty;
					batchAvailability[promotionBatchId] = available - useQty;
				}
			}

			if (remainingNeed > 0)
			{
				allocations.Add(new CheckoutLineAllocation(CreateSplitItem(line, remainingNeed, batchId: null), false));
			}

			return (allocations, allowedQty);
		}

		private sealed record CheckoutLineAllocation(CartCheckoutItemDto Item, bool IsEligible);

		private async Task<Dictionary<Guid, List<PromotionItem>>> GetActivePromotionsByVariantAsync(Guid campaignId, PromotionType itemType, List<Guid> variantIds)
		{
			var promotionItems = await _unitOfWork.PromotionItems.GetAllAsync(
				i => i.CampaignId == campaignId && !i.IsDeleted && i.ItemType == itemType && variantIds.Contains(i.ProductVariantId) && i.IsActive,
				asNoTracking: true);
			return promotionItems.GroupBy(x => x.ProductVariantId).ToDictionary(g => g.Key, g => g.ToList());
		}

		private async Task<Dictionary<Guid, int>> GetBatchAvailabilityAsync(IEnumerable<PromotionItem> promotionItems)
		{
			var batchIds = promotionItems.Where(x => x.BatchId.HasValue).Select(x => x.BatchId!.Value).Distinct().ToList();
			if (batchIds.Count == 0) return [];

			var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id), asNoTracking: true);
			return batches.ToDictionary(b => b.Id, b => Math.Max(0, b.RemainingQuantity - b.ReservedQuantity));
		}

		private static decimal CalculateDiscountAmount(VoucherResponse voucher, decimal eligibleSubtotal)
		{
			var discountAmount = voucher.DiscountType switch
			{
				DiscountType.Percentage => eligibleSubtotal * (voucher.DiscountValue / 100m),
				DiscountType.FixedAmount => voucher.DiscountValue,
				_ => 0m
			};
			return Math.Min(discountAmount, eligibleSubtotal);
		}
	}
}
