using Microsoft.EntityFrameworkCore;
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
		private readonly IStockReservationService _stockReservationService;

		public CartItemService(
			IUnitOfWork unitOfWork,
			IStockReservationService stockReservationService)
		{
			_unitOfWork = unitOfWork;
			_stockReservationService = stockReservationService;
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

			await EnsureCartQuantityWithinSellableLimitsAsync(request.VariantId, totalQuantity);

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

			await EnsureCartQuantityWithinSellableLimitsAsync(cartItem.VariantId, request.Quantity);

			cartItem.SetQuantity(request.Quantity);
			_unitOfWork.CartItems.Update(cartItem);

			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Cập nhật sản phẩm trong giỏ hàng thành công");
		}

		private async Task EnsureCartQuantityWithinSellableLimitsAsync(Guid variantId, int requiredQuantity)
		{
			var stock = await _unitOfWork.Stocks.FirstOrDefaultAsync(s => s.VariantId == variantId)
				?? throw AppException.NotFound("Không tìm thấy tồn kho cho biến thể sản phẩm.");

			if (stock.AvailableQuantity < requiredQuantity)
				throw AppException.BadRequest("Không đủ tồn kho cho số lượng yêu cầu.");

			var (aggregated, safeOnly, hasClearanceBypass) = await _stockReservationService.GetVariantSellableSnapshotForCartAsync(variantId);
			var maxByBatch = hasClearanceBypass ? aggregated : safeOnly;

			var sellable = Math.Min(stock.AvailableQuantity, maxByBatch);
			if (requiredQuantity > sellable)
				throw AppException.BadRequest("Không đủ tồn kho cho số lượng yêu cầu.");
		}
	}
}
