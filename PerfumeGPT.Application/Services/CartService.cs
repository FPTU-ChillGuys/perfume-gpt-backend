using Microsoft.EntityFrameworkCore;
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
			List<CartCheckoutItemDto> pricedItems;
			decimal subtotal;
			decimal finalAmount;
			string? voucherMessage;
			var appliedVoucherCode = request.VoucherCode;

			try
			{
				(pricedItems, subtotal, finalAmount, voucherMessage) = await CalculatePricingEngineAsync(
					checkoutItems,
					request.VoucherCode,
					request.CustomerId);
			}
			catch (AppException ex) when (IsVoucherSoftFailForPreview(ex, request.VoucherCode))
			{
				(pricedItems, subtotal, finalAmount, voucherMessage) = await CalculatePricingEngineAsync(
					checkoutItems,
					null,
					request.CustomerId);

				appliedVoucherCode = null;
				var voucherErrorMessage = $"Voucher '{request.VoucherCode}' is invalid. Preview returned without voucher discount.";
				voucherMessage = string.IsNullOrWhiteSpace(voucherMessage)
					? voucherErrorMessage
					: $"{voucherMessage} | {voucherErrorMessage}";
			}

			// 4. MAP SANG DTO HIỂN THỊ (Tách bạch UI)
			var responseItems = pricedItems.Select(item =>
			{
				// TÁCH BẠCH: Tiền giảm của món hàng = Tổng giảm - Phần gánh Voucher
				var promoDiscountOnly = item.Discount - item.ApportionedVoucherDiscount;
				var lineTotalBeforeVoucher = item.SubTotal - promoDiscountOnly;

				return new PosOrderDetailListItem
				{
					VariantId = item.VariantId,
					BatchId = item.BatchId!.Value,
					VariantName = item.VariantName,
					Quantity = item.Quantity,
					UnitPrice = item.UnitPrice,
					SubTotal = item.SubTotal, // Giá gốc (VD: 1.580.000)

					// CHỈ HIỂN THỊ GIÁ TRỊ CỦA PROMOTION
					Discount = promoDiscountOnly,
					FinalTotal = lineTotalBeforeVoucher,

					ImageUrl = item.ImageUrl ?? "",
					BatchCode = item.BatchCode ?? throw AppException.BadRequest("Batch code is required for POS items.")
				};
			}).ToList();

			var response = new PreviewPosOrderResponse
			{
				Items = responseItems,

				// Tổng của đơn hàng giữ nguyên logic cũ, rất chuẩn xác:
				SubTotal = subtotal, // Đây là tổng tiền sau khi đã trừ Promotion của các Items
				Discount = subtotal - finalAmount, // Đây chính xác là tiền giảm của Voucher
				TotalPrice = finalAmount
			};

			if (!string.IsNullOrWhiteSpace(request.SessionId))
			{
				var customerDisplayData = new CartDisplayDto
				{
					Items = response.Items,
					SubTotal = response.SubTotal,
					Discount = response.Discount,
					TotalPrice = response.TotalPrice,
					VoucherCode = appliedVoucherCode,
				};

				await _signalRService.UpdateCustomerDisplayAsync(request.SessionId, customerDisplayData);
			}

			return BaseResponse<PreviewPosOrderResponse>.Ok(response, string.IsNullOrWhiteSpace(voucherMessage) ? "Order previewed successfully." : voucherMessage);
		}

		private static bool IsVoucherSoftFailForPreview(AppException ex, string? voucherCode)
		{
			if (string.IsNullOrWhiteSpace(voucherCode))
				return false;

			var isVoucherErrorType = ex.ErrorType is ResponseErrorType.NotFound or ResponseErrorType.BadRequest;
			if (!isVoucherErrorType)
				return false;

			return ex.Message.Contains("voucher", StringComparison.OrdinalIgnoreCase);
		}


		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId, List<Guid>? itemIds, bool saveChanges = true)
		{
			var hasItems = await _unitOfWork.CartItems.HasItemsAsync(userId);
			if (hasItems)
			{
				await _unitOfWork.CartItems.ClearCartByUserIdAsync(userId, itemIds);

				if (saveChanges)
				{
					var saved = await _unitOfWork.SaveChangesAsync();
					if (!saved)
					{
						throw AppException.Internal("Could not clear cart");
					}
				}
			}

			return BaseResponse<string>.Ok("Cart cleared successfully");
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
			// 1. Lấy dữ liệu thô từ DB
			var rawItems = await _unitOfWork.CartItems.GetCartItemsByUserIdAsync(userId, request.ItemIds);
			if (rawItems == null || rawItems.Count == 0)
			{
				return BaseResponse<GetCartItemsResponse>.Ok(
					new GetCartItemsResponse { Items = [] },
					"Cart is empty");
			}

			// 2. Chuyển đổi sang định dạng của Pricing Engine
			var checkoutItems = rawItems.Select(item => new CartCheckoutItemDto
			{
				VariantId = item.VariantId,
				VariantName = item.VariantName,
				Quantity = item.Quantity,
				UnitPrice = item.VariantPrice,
				SubTotal = item.SubTotal,
				Discount = 0m,
				FinalTotal = item.SubTotal
			}).ToList();

			// 3. Chạy Pricing Engine (Truyền VoucherCode = null để chỉ lấy Promotion)
			var (pricedItems, _, _, _) = await CalculatePricingEngineAsync(checkoutItems, null, userId);
			var pricedItemByVariant = pricedItems
				.GroupBy(x => x.VariantId)
				.ToDictionary(g => g.Key, g => g.First());

			// 4. Map kết quả từ Engine ngược lại vào Response Items
			var responseItems = rawItems.Select(rawItem =>
			 {
				 if (pricedItemByVariant.TryGetValue(rawItem.VariantId, out var pricedItem))
				 {
					 return rawItem with
					 {
						 Discount = pricedItem.Discount,
						 FinalTotal = pricedItem.FinalTotal
					 };
				 }

				 return rawItem with
				 {
					 Discount = 0m,
					 FinalTotal = rawItem.SubTotal
				 };
			 }).ToList();

			return BaseResponse<GetCartItemsResponse>.Ok(
			  new GetCartItemsResponse { Items = responseItems },
				"Cart items retrieved successfully");
		}

		public async Task<BaseResponse<GetCartTotalResponse>> GetCartTotalAsync(Guid userId, GetCartTotalRequest request)
		{
			var (items, engineSubtotal, finalAmount, voucherMessage) = await BuildCheckoutPricingAsync(userId, request);

			if (items.Count == 0)
			{
				return BaseResponse<GetCartTotalResponse>.Ok(
					new GetCartTotalResponse
					{
						Subtotal = 0m,
						ShippingFee = 0m,
						Discount = 0m,
						TotalPrice = 0m
					},
					"Cart is empty");
			}

			// TÁCH BẠCH DISCOUNT VOUCHER VÀ PROMOTION
			// 1. Tiền giảm của Promotion = Tổng Discount - Phần gánh Voucher
			var totalPromoDiscount = items.Sum(x => x.Discount - x.ApportionedVoucherDiscount);

			// 2. Subtotal hiển thị = Tổng giá gốc - Tiền Promotion (Sẽ ra đúng 5.222.000)
			var subTotalAfterPromo = items.Sum(x => x.SubTotal) - totalPromoDiscount;

			// 3. Discount hiển thị = Tiền Voucher (Sẽ ra đúng 100.000)
			var voucherDiscount = items.Sum(x => x.ApportionedVoucherDiscount);

			var response = new GetCartTotalResponse
			{
				Subtotal = subTotalAfterPromo,
				ShippingFee = 0m, // Logic phí ship của bạn
				Discount = voucherDiscount,
				TotalPrice = finalAmount
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
			var (itemsAfterFlashSale, flashSaleMessage) = await ApplyAutoFlashSalesAsync(checkoutItems);
			var subtotal = itemsAfterFlashSale.Sum(x => x.FinalTotal); // Subtotal MỚI sau khi đã trừ Flash Sale

			// PHASE 2 & 3: XỬ LÝ VOUCHER
			if (string.IsNullOrWhiteSpace(voucherCode))
				return (itemsAfterFlashSale, subtotal, subtotal, flashSaleMessage);

			var voucher = await _voucherService.GetVoucherByCodeAsync(voucherCode)
				?? throw AppException.NotFound("Voucher not found");

			var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
			   voucherCode, userId, subtotal, null, itemsAfterFlashSale.Select(x => x.VariantId));

			if (!voucherValidation)
				throw AppException.BadRequest("Voucher validation failed.");

			if (voucher.ApplyType == VoucherType.Product && voucher.CampaignId.HasValue)
			{
				var (pricedItems, message) = await ApplyProductLevelVoucherDiscountAsync(voucher, itemsAfterFlashSale);
				var finalAmount = pricedItems.Sum(x => x.FinalTotal);
				return (pricedItems, subtotal, finalAmount, message);
			}

			var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucherCode, subtotal);
			var totalDiscount = subtotal - discountedTotal;

			var adjustedItems = ApplyProportionalDiscount(itemsAfterFlashSale, totalDiscount, x => x.FinalTotal);
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

			return await CalculatePricingEngineAsync(checkoutItems, request.VoucherCode, userId);
		}

		private async Task<(List<CartCheckoutItemDto> Items, string? Message)> ApplyProductLevelVoucherDiscountAsync(VoucherResponse voucher, List<CartCheckoutItemDto> items)
		{
			if (!voucher.CampaignId.HasValue)
			{
				throw AppException.BadRequest("Product-level voucher must be associated with a campaign.");
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
					: Math.Round((target.Weight / weightTotal) * totalDiscount, 0, MidpointRounding.AwayFromZero);

				var lineBaseAmount = target.Item.FinalTotal;
				var safeDiscount = Math.Max(0m, Math.Min(rawDiscount, lineBaseAmount));
				allocated += safeDiscount;

				result[target.Index] = target.Item with
				{
					Discount = target.Item.Discount + safeDiscount,

					// CẬP NHẬT: Lưu vết số tiền Voucher mà dòng này phải gánh
					ApportionedVoucherDiscount = target.Item.ApportionedVoucherDiscount + safeDiscount,

					FinalTotal = lineBaseAmount - safeDiscount
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
				.Where(x => x.IsEligible && x.Item.FinalTotal > 0)
				.ToList();

			if (weightedItems.Count == 0)
				return result;

			var weightTotal = weightedItems.Sum(x => x.Item.FinalTotal);
			if (weightTotal <= 0)
				return result;

			decimal allocated = 0m;
			for (var i = 0; i < weightedItems.Count; i++)
			{
				var target = weightedItems[i];
				var isLast = i == weightedItems.Count - 1;
				var rawDiscount = isLast
					? totalDiscount - allocated
				   : Math.Round((target.Item.FinalTotal / weightTotal) * totalDiscount, 0, MidpointRounding.AwayFromZero);

				var lineBaseAmount = target.Item.FinalTotal;
				var safeDiscount = Math.Max(0m, Math.Min(rawDiscount, lineBaseAmount));
				allocated += safeDiscount;

				result[target.Index] = target.Item with
				{
					Discount = target.Item.Discount + safeDiscount,

					// CẬP NHẬT: Lưu vết số tiền Voucher mà dòng này phải gánh
					ApportionedVoucherDiscount = target.Item.ApportionedVoucherDiscount + safeDiscount,

					FinalTotal = lineBaseAmount - safeDiscount
				};
			}

			return result;
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
						eligibleSubtotal += line.FinalTotal;
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
							var eligibleLine = CreateSplitItem(line, allowedQty, line.BatchId);
							allocations.Add(new CheckoutLineAllocation(eligibleLine, true));
							eligibleSubtotal += eligibleLine.FinalTotal;
						}

						var excludedQty = line.Quantity - allowedQty;
						if (excludedQty > 0)
						{
							var ineligibleLine = CreateSplitItem(line, excludedQty, line.BatchId);
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
				 .Sum(x => x.Item.FinalTotal);

				var excludedQtyOnline = line.Quantity - allowedQtyOnline;

				if (excludedQtyOnline > 0)
					messageLines.Add($"'{line.VariantName}': only {allowedQtyOnline} in promotion, {excludedQtyOnline} not.");
			}

			return (allocations, eligibleSubtotal, messageLines);
		}

		private static CartCheckoutItemDto CreateSplitItem(CartCheckoutItemDto source, int quantity, Guid? batchId)
		{
			if (quantity <= 0)
			{
				return source with
				{
					BatchId = batchId,
					Quantity = 0,
					SubTotal = 0m,
					Discount = 0m,
					FinalTotal = 0m
				};
			}

			if (quantity == source.Quantity)
			{
				return source with { BatchId = batchId };
			}

			var sourceQty = Math.Max(1, source.Quantity);
			var unitSubTotal = source.SubTotal / sourceQty;
			var unitFinalTotal = source.FinalTotal / sourceQty;
			var subTotal = Math.Round(unitSubTotal * quantity, 0, MidpointRounding.AwayFromZero);
			var finalTotal = Math.Round(unitFinalTotal * quantity, 0, MidpointRounding.AwayFromZero);
			return source with
			{
				BatchId = batchId,
				Quantity = quantity,
				SubTotal = subTotal,
				Discount = Math.Max(0m, subTotal - finalTotal),
				FinalTotal = finalTotal
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
				i => i.CampaignId == campaignId
				  && !i.IsDeleted
				  && i.ItemType == itemType
				  && variantIds.Contains(i.TargetProductVariantId)
				  && i.IsActive
				  && (!i.MaxUsage.HasValue || i.CurrentUsage < i.MaxUsage.Value),
				asNoTracking: true);

			return promotionItems.GroupBy(x => x.TargetProductVariantId).ToDictionary(g => g.Key, g => g.ToList());
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
			var rawDiscountAmount = voucher.DiscountType switch
			{
				DiscountType.Percentage => Math.Round(eligibleSubtotal * (voucher.DiscountValue / 100m), 0, MidpointRounding.AwayFromZero),
				DiscountType.FixedAmount => voucher.DiscountValue,
				_ => 0m
			};

			if (voucher.MaxDiscountAmount.HasValue && voucher.MaxDiscountAmount.Value > 0)
			{
				rawDiscountAmount = Math.Min(rawDiscountAmount, voucher.MaxDiscountAmount.Value);
			}

			return Math.Min(rawDiscountAmount, eligibleSubtotal);
		}

		private async Task<(List<CartCheckoutItemDto> Items, string? Message)> ApplyAutoFlashSalesAsync(List<CartCheckoutItemDto> items)
		{
			if (items.Count == 0)
				return ([], null);

			var variantIds = items.Select(x => x.VariantId).Distinct().ToList();
			var now = DateTime.UtcNow;

			// 1. TÌM PROMOTION ĐANG ACTIVE
			var activePromotions = await _unitOfWork.PromotionItems.GetAllAsync(
				p => variantIds.Contains(p.TargetProductVariantId)
				  && p.IsActive
				  && !p.IsDeleted
				  && p.Campaign.Status == CampaignStatus.Active
				  && p.Campaign.StartDate <= now
				  && p.Campaign.EndDate >= now
				  && (!p.MaxUsage.HasValue || p.CurrentUsage < p.MaxUsage.Value),
				include: query => query.Include(p => p.Campaign),
				asNoTracking: true);

			if (!activePromotions.Any())
				return (items.ToList(), null);

			var promoByVariant = activePromotions.GroupBy(p => p.TargetProductVariantId).ToDictionary(g => g.Key, g => g.ToList());
			var remainingPromotionQuota = activePromotions
				.ToDictionary(
					p => p.Id,
					p => p.MaxUsage.HasValue
						? Math.Max(0, p.MaxUsage.Value - p.CurrentUsage)
						: int.MaxValue);

			// 2. LẤY TỒN KHO THỰC TẾ (Để biết lô nào còn hàng mà chia)
			var batchAvailability = await GetBatchAvailabilityAsync(activePromotions);

			var resultItems = new List<CartCheckoutItemDto>();
			var messageLines = new List<string>();

			// 3. LẶP QUA GIỎ HÀNG VÀ XỬ LÝ PHÂN BỔ
			foreach (var item in items)
			{
				if (!promoByVariant.TryGetValue(item.VariantId, out var variantPromos))
				{
					resultItems.Add(item);
					continue;
				}

				// TRƯỜNG HỢP 1: Có Khuyến mãi Global (Không trói buộc Batch)
				var globalPromos = variantPromos.Where(p => !p.BatchId.HasValue).ToList();
				if (globalPromos.Any())
				{
					var bestPromo = globalPromos.OrderByDescending(p => CalculateFlashSaleDiscount(item.UnitPrice, p)).First();

					// KIỂM TRA QUOTA KHUYẾN MÃI TẠI ĐÂY
					var promoQuota = remainingPromotionQuota.TryGetValue(bestPromo.Id, out var quota)
						? quota
						: 0;

					if (promoQuota > 0)
					{
						var qtyToDiscount = Math.Min(item.Quantity, promoQuota);

						// Cập nhật quota tạm thời trong RAM để vòng lặp sau không lấy lố
						remainingPromotionQuota[bestPromo.Id] = Math.Max(0, promoQuota - qtyToDiscount);

						if (qtyToDiscount == item.Quantity)
						{
							// Được giảm toàn bộ
							resultItems.Add(ApplyDiscountToItem(item, bestPromo, ref messageLines, qtyToDiscount));
						}
						else
						{
							// Bị cắt làm đôi: Một phần giảm giá, phần dư giữ giá gốc
							var discountedSplit = CreateSplitItem(item, qtyToDiscount, item.BatchId);
							resultItems.Add(ApplyDiscountToItem(discountedSplit, bestPromo, ref messageLines, qtyToDiscount));

							var originalPriceSplit = CreateSplitItem(item, item.Quantity - qtyToDiscount, item.BatchId);
							resultItems.Add(originalPriceSplit);
						}
					}
					else
					{
						resultItems.Add(item); // Quota đã hết
					}
					continue;
				}

				// TRƯỜNG HỢP 2: Khách mua Online (BatchId = null) NHƯNG chỉ có Khuyến mãi theo Lô
				if (!item.BatchId.HasValue)
				{
					// Lấy các khuyến mãi Lô và xếp hạng "Best Deal"
					var batchPromos = variantPromos.Where(p => p.BatchId.HasValue)
						.OrderByDescending(p => CalculateFlashSaleDiscount(item.UnitPrice, p))
						.ToList();

					int remainingQty = item.Quantity;

					// Vắt kiệt số lượng từ các Lô có khuyến mãi
					foreach (var promo in batchPromos)
					{
						if (remainingQty <= 0) break;

						var promoBatchId = promo.BatchId!.Value;
						if (batchAvailability.TryGetValue(promoBatchId, out var available) && available > 0)
						{
							var qtyToTake = Math.Min(remainingQty, available);

							// Trừ tồn kho tạm thời trong RAM để vòng lặp sau không lấy lố
							batchAvailability[promoBatchId] -= qtyToTake;
							remainingQty -= qtyToTake;

							// Tách dòng và ép gán BatchId ngay tại đây
							var splitItem = CreateSplitItem(item, qtyToTake, promoBatchId);
							resultItems.Add(ApplyDiscountToItem(splitItem, promo, ref messageLines, qtyToTake));
						}
					}

					// Nếu đã vét sạch các lô khuyến mãi mà khách đặt nhiều quá -> Phần dư giữ nguyên giá gốc
					if (remainingQty > 0)
					{
						resultItems.Add(CreateSplitItem(item, remainingQty, null));
					}
				}
				// TRƯỜNG HỢP 3: Mua tại POS (Đã quét mã vạch trúng Lô cụ thể)
				else
				{
					var specificPromo = variantPromos.Where(p => p.BatchId == item.BatchId).ToList();
					if (specificPromo.Any())
					{
						var bestPromo = specificPromo.OrderByDescending(p => CalculateFlashSaleDiscount(item.UnitPrice, p)).First();
						resultItems.Add(ApplyDiscountToItem(item, bestPromo, ref messageLines, item.Quantity));
					}
					else
					{
						resultItems.Add(item);
					}
				}
			}

			var message = messageLines.Count > 0 ? string.Join(" | ", messageLines) : null;
			return (resultItems, message);
		}

		//  Hàm Helper để code gọn gàng (Bạn thêm hàm này vào dưới hàm CalculateFlashSaleDiscount)
		private static CartCheckoutItemDto ApplyDiscountToItem(CartCheckoutItemDto item, PromotionItem promo, ref List<string> messageLines, int discountedQuantity)
		{
			// Tính số tiền giảm cho 1 sản phẩm
			var discountPerItem = CalculateFlashSaleDiscount(item.UnitPrice, promo);

			// Tổng giảm = Giá giảm 1 món * Số lượng món được phép giảm
			var totalDiscountForLine = discountPerItem * discountedQuantity;
			var newFinalTotal = item.SubTotal - totalDiscountForLine;

			messageLines.Add($"'{item.VariantName}' áp dụng {promo.Campaign.Name} (Giảm {discountPerItem:N0}/sp cho {discountedQuantity} sản phẩm)");

			return item with
			{
				Discount = item.Discount + totalDiscountForLine,
				FinalTotal = newFinalTotal
			};
		}

		// Hàm Helper tính toán mức giảm cho 1 sản phẩm
		private static decimal CalculateFlashSaleDiscount(decimal unitPrice, PromotionItem promo)
		{
			var rawDiscount = promo.DiscountType == DiscountType.Percentage
				? Math.Round(unitPrice * (promo.DiscountValue / 100m), 0, MidpointRounding.AwayFromZero)
				: promo.DiscountValue;

			// Đảm bảo không giảm lố giá gốc của sản phẩm (Vd: Sp 100k mà flash sale giảm 150k thì chỉ giảm 100k)
			return Math.Min(rawDiscount, unitPrice);
		}
	}
}
