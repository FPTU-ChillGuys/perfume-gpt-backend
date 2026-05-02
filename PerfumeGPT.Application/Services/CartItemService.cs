using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CartItemService : ICartItemService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IStockService _stockService;

		public CartItemService(
			IUnitOfWork unitOfWork,
			IStockService stockService)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> AddToCartAsync(Guid userId, CreateCartItemRequest request)
		{
			var variant = await _unitOfWork.Variants.GetByIdAsync(request.VariantId)
				?? throw AppException.NotFound("Không tìm thấy biến thể sản phẩm");

			var existing = await _unitOfWork.CartItems.FirstOrDefaultAsync(
				ci => ci.UserId == userId && ci.VariantId == request.VariantId);

			variant.EnsureAvailableForCart();

			var totalQuantity = existing != null ? existing.Quantity + request.Quantity : request.Quantity;
			// Trong CartItemService.cs -> AddToCartAsync
			var storePolicy = await _unitOfWork.StorePolicies.GetCurrentPolicyAsync();
			var bufferDays = storePolicy?.StopSellingBeforeExpiryDays;

			var now = DateTime.UtcNow;
           // Lấy các Batch đang chạy Xả kho
			var activeClearancePromotions = await _unitOfWork.PromotionItems
				.GetActiveClearancePromotionsByVariantIdAsync(request.VariantId, now);

			var exemptedBatchIds = activeClearancePromotions.Select(p => p.BatchId!.Value).ToList();

			var hasStock = await _stockService.HasSufficientStockAsync(request.VariantId, totalQuantity, bufferDays, exemptedBatchIds);
			if (!hasStock)
			{
				throw AppException.BadRequest("Không đủ tồn kho cho số lượng yêu cầu");
			}

			if (existing != null)
			{
				existing.SetQuantity(totalQuantity);
				_unitOfWork.CartItems.Update(existing);

				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<string>.Ok(existing.Id.ToString(), "Cập nhật số lượng sản phẩm trong giỏ hàng thành công");
			}

			var cartItem = CartItem.Create(userId, request.VariantId, request.Quantity);
			await _unitOfWork.CartItems.AddAsync(cartItem);

			try
			{
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex) when (ex.InnerException?.Message.Contains("UNIQUE") == true || ex.InnerException?.Message.Contains("Duplicate") == true)
			{
				throw AppException.Conflict("Sản phẩm đã được thêm vào giỏ hàng bởi một yêu cầu khác. Vui lòng tải lại trang.");
			}

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Thêm sản phẩm vào giỏ hàng thành công");
		}

		public async Task<BaseResponse<string>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
		{
			var cartItem = await _unitOfWork.CartItems.GetByIdAsync(cartItemId) ?? throw AppException.NotFound("Không tìm thấy sản phẩm trong giỏ hàng");

			if (!cartItem.IsOwnedBy(userId))
				throw AppException.Forbidden("Sản phẩm trong giỏ hàng không thuộc về người dùng");

			_unitOfWork.CartItems.Remove(cartItem);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Xóa sản phẩm khỏi giỏ hàng thành công");
		}

		public async Task<BaseResponse<string>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
		{
			var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(ci => ci.Id == cartItemId && ci.UserId == userId)
				?? throw AppException.NotFound("Không tìm thấy sản phẩm trong giỏ hàng");

			if (request.Quantity <= 0)
			{
				_unitOfWork.CartItems.Remove(cartItem);
				await _unitOfWork.SaveChangesAsync();
				return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Xóa sản phẩm khỏi giỏ hàng thành công");
			}

			var bufferDays = (await _unitOfWork.StorePolicies.GetCurrentPolicyAsync())?.StopSellingBeforeExpiryDays;
			var now = DateTime.UtcNow;
			var activeClearancePromotions = await _unitOfWork.PromotionItems
				.GetActiveClearancePromotionsByVariantIdAsync(cartItem.VariantId, now);
			var exemptedBatchIds = activeClearancePromotions.Select(p => p.BatchId!.Value).ToList();

			var hasStock = await _stockService.HasSufficientStockAsync(cartItem.VariantId, request.Quantity, bufferDays, exemptedBatchIds);
			if (!hasStock)
			{
				throw AppException.BadRequest("Không đủ tồn kho cho số lượng yêu cầu");
			}

			cartItem.SetQuantity(request.Quantity);
			_unitOfWork.CartItems.Update(cartItem);

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Cập nhật sản phẩm trong giỏ hàng thành công");
		}
	}
}
