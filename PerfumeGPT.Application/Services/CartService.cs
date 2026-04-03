using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class CartService : ICartService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVoucherService _voucherService;

		public CartService(
			IUnitOfWork unitOfWork,
			IVoucherService voucherService)
		{
			_unitOfWork = unitOfWork;
			_voucherService = voucherService;
		}
		#endregion Dependencies

		public async Task<CartCheckoutResponse> GetCartForCheckoutAsync(Guid userId, GetCartTotalRequest request)
		{
			var (items, subtotal, finalAmount, _) = await BuildCheckoutPricingAsync(userId, request);
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

		private async Task<(List<CartCheckoutItemDto> Items, string? Message)> ApplyProductLevelVoucherDiscountAsync(
			   VoucherResponse voucher,
			  List<CartCheckoutItemDto> items)
		{
			if (!voucher.CampaignId.HasValue)
			{
				var subtotal = items.Sum(x => x.SubTotal);
				var discountedTotal = await _voucherService.CalculateVoucherDiscountAsync(voucher.Code, subtotal);
				var totalDiscount = subtotal - discountedTotal;
				return (ApplyProportionalDiscount(items, totalDiscount, x => x.SubTotal), null);
			}

			var now = DateTime.UtcNow;
			var variantIds = items.Select(x => x.VariantId).Distinct().ToList();

			var promotionItems = (await _unitOfWork.PromotionItems.GetAllAsync(
				i => i.CampaignId == voucher.CampaignId.Value
					&& !i.IsDeleted
					&& i.ItemType == voucher.TargetItemType
					&& variantIds.Contains(i.ProductVariantId)
					&& (i.IsActive),
				asNoTracking: true)).ToList();

			if (promotionItems.Count == 0)
			{
				return (items, "No eligible product is in active promotion for this voucher");
			}

			var promoItemsByVariant = promotionItems
				   .GroupBy(x => x.ProductVariantId)
				   .ToDictionary(g => g.Key, g => g.ToList());

			var batchIds = promotionItems
				.Where(x => x.BatchId.HasValue)
				.Select(x => x.BatchId!.Value)
				.Distinct()
				.ToList();

			var batchAvailability = new Dictionary<Guid, int>();
			if (batchIds.Count > 0)
			{
				var batches = await _unitOfWork.Batches.GetAllAsync(b => batchIds.Contains(b.Id), asNoTracking: true);
				batchAvailability = batches.ToDictionary(b => b.Id, b => Math.Max(0, b.RemainingQuantity - b.ReservedQuantity));
			}

			var eligibleQty = 0;
			decimal eligibleSubtotal = 0m;
			var eligibleLineSubtotals = new Dictionary<Guid, decimal>();
			var messageLines = new List<string>();

			foreach (var line in items)
			{
				if (!promoItemsByVariant.TryGetValue(line.VariantId, out var variantPromotions) || variantPromotions.Count == 0)
				{
					messageLines.Add($"'{line.VariantName}': {line.Quantity} not in promotion.");
					continue;
				}

				var hasGlobalPromotion = variantPromotions.Any(x => !x.BatchId.HasValue);
				var allowedQty = hasGlobalPromotion ? line.Quantity : 0;

				if (!hasGlobalPromotion)
				{
					var remainingNeed = line.Quantity;
					foreach (var promotion in variantPromotions.Where(x => x.BatchId.HasValue))
					{
						if (remainingNeed <= 0)
						{
							break;
						}

						if (!batchAvailability.TryGetValue(promotion.BatchId!.Value, out var availableInBatch) || availableInBatch <= 0)
						{
							continue;
						}

						var useQty = Math.Min(remainingNeed, availableInBatch);
						allowedQty += useQty;
						remainingNeed -= useQty;
						batchAvailability[promotion.BatchId.Value] = availableInBatch - useQty;
					}
				}

				var excludedQty = line.Quantity - allowedQty;
				eligibleQty += allowedQty;
				var eligibleLineSubtotal = line.UnitPrice * allowedQty;
				eligibleSubtotal += eligibleLineSubtotal;

				if (eligibleLineSubtotal > 0)
					eligibleLineSubtotals[line.VariantId] = eligibleLineSubtotal;

				if (excludedQty > 0)
				{
					messageLines.Add($"'{line.VariantName}': only {allowedQty} in promotion, {excludedQty} not.");
				}
			}

			if (eligibleQty <= 0)
			{
				return (items, "No quantity is available in promotion batch for voucher discount");
			}

			var discountAmount = voucher.DiscountType switch
			{
				DiscountType.Percentage => eligibleSubtotal * (voucher.DiscountValue / 100m),
				DiscountType.FixedAmount => voucher.DiscountValue,
				_ => 0m
			};

			if (discountAmount > eligibleSubtotal)
			{
				discountAmount = eligibleSubtotal;
			}

			var discountedItems = ApplyProportionalDiscount(
				items,
				discountAmount,
				x => eligibleLineSubtotals.TryGetValue(x.VariantId, out var value) ? value : 0m);

			var message = messageLines.Count > 0 ? string.Join(" | ", messageLines) : null;

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

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId, List<Guid>? itemIds)
		{
			var hasItems = await _unitOfWork.CartItems.HasItemsAsync(userId);
			if (hasItems)
			{
				await _unitOfWork.CartItems.ClearCartByUserIdAsync(userId, itemIds);
			}
			return BaseResponse<string>.Ok("Cart cleared successfully");
		}
	}
}
