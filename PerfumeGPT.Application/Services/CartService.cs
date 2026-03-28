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
			var items = await _unitOfWork.CartItems.GetCartCheckoutItemsAsync(userId, request.ItemIds);
			if (items == null || items.Count == 0)
			{
				return new CartCheckoutResponse
				{
					Items = [],
					ShippingFee = 0m,
					TotalPrice = 0m
				};
			}

			var totalResult = await GetCartTotalAsync(userId, request);
			if (!totalResult.Success || totalResult.Payload == null)
			{
				throw AppException.BadRequest(totalResult.Message);
			}

			var response = new CartCheckoutResponse
			{
				Items = items,
				ShippingFee = totalResult.Payload.ShippingFee,
				TotalPrice = totalResult.Payload.TotalPrice
			};

			return response;
		}

		public async Task<BaseResponse<GetCartItemsResponse>> GetCartItemsAsync(Guid userId, List<Guid>? itemIds = null)
		{
			var items = await _unitOfWork.CartItems.GetCartItemsByUserIdAsync(userId, itemIds);
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
			// 1. Get cart items
			var items = await _unitOfWork.CartItems.GetCartItemPricesAsync(userId, request.ItemIds);
			if (items == null || items.Count == 0)
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

			// 2. Calculate subtotal
			var subtotal = items.Sum(item => item.SubTotal);

			// 3. Apply voucher discount if provided
			decimal finalAmount = subtotal;
			string? voucherMessage = null;
			if (!string.IsNullOrWhiteSpace(request.VoucherCode))
			{

				var voucher = await _voucherService.GetVoucherByCodeAsync(request.VoucherCode) ?? throw AppException.NotFound("Voucher not found");

				var voucherValidation = await _voucherService.CanUserApplyVoucherAsync(
					request.VoucherCode,
					userId,
					subtotal,
					null,  // Cart for signed-in users, not guests
					items.Select(x => x.VariantId));

				if (!voucherValidation)
					throw AppException.BadRequest("Voucher validation failed");

				if (voucher.ApplyType == VoucherType.Product && voucher.CampaignId.HasValue)
				{
					var (CalculatedfinalAmount, message) = await CalculateProductLevelVoucherAmountAsync(voucher, items);
					finalAmount = CalculatedfinalAmount;
					voucherMessage = message;
				}
				else
				{
					finalAmount = await _voucherService.CalculateVoucherDiscountAsync(
						request.VoucherCode,
						subtotal);
				}
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

		private async Task<(decimal FinalAmount, string? Message)> CalculateProductLevelVoucherAmountAsync(
			VoucherResponse voucher,
			List<CartItemPriceDto> items)
		{
			if (!voucher.CampaignId.HasValue)
			{
				var fallback = await _voucherService.CalculateVoucherDiscountAsync(voucher.Code, items.Sum(x => x.SubTotal));
				return (fallback, null);
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
				return (items.Sum(x => x.SubTotal), "No eligible product is in active promotion for this voucher");
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
			var nonEligibleQty = 0;
			decimal eligibleSubtotal = 0m;
			decimal nonEligibleSubtotal = 0m;
			var messageLines = new List<string>();

			foreach (var line in items)
			{
				if (!promoItemsByVariant.TryGetValue(line.VariantId, out var variantPromotions) || variantPromotions.Count == 0)
				{
					nonEligibleQty += line.Quantity;
					nonEligibleSubtotal += line.SubTotal;
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
				nonEligibleQty += excludedQty;
				eligibleSubtotal += line.VariantPrice * allowedQty;
				nonEligibleSubtotal += line.VariantPrice * excludedQty;

				if (excludedQty > 0)
				{
					messageLines.Add($"'{line.VariantName}': only {allowedQty} in promotion, {excludedQty} not.");
				}
			}

			if (eligibleQty <= 0)
			{
				return (eligibleSubtotal + nonEligibleSubtotal, "No quantity is available in promotion batch for voucher discount");
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

			var finalAmount = (eligibleSubtotal - discountAmount) + nonEligibleSubtotal;
			var message = messageLines.Count > 0 ? string.Join(" | ", messageLines) : null;

			return (finalAmount, message);
		}

		public async Task<BaseResponse<string>> ClearCartAsync(Guid userId, List<Guid>? itemIds)
		{
			var hasItems = await _unitOfWork.CartItems.HasItemsAsync(userId);
			if (hasItems)
			{
				var result = await _unitOfWork.CartItems.ClearCartByUserIdAsync(userId, itemIds);
				if (!result)
				{
					throw AppException.Internal("Could not clear cart");
				}
			}
			return BaseResponse<string>.Ok("Cart cleared successfully");
		}
	}
}
