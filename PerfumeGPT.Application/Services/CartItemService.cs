using FluentValidation;
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
		private readonly IValidator<CreateCartItemRequest> _createCartItemValidator;
		private readonly IValidator<UpdateCartItemRequest> _updateCartItemValidator;

		public CartItemService(
			IUnitOfWork unitOfWork,
			IStockService stockService,
			IValidator<CreateCartItemRequest> createCartItemValidator,
			IValidator<UpdateCartItemRequest> updateCartItemValidator)
		{
			_unitOfWork = unitOfWork;
			_stockService = stockService;
			_createCartItemValidator = createCartItemValidator;
			_updateCartItemValidator = updateCartItemValidator;
		}

		#endregion

		public async Task<BaseResponse<string>> AddToCartAsync(Guid userId, CreateCartItemRequest request)
		{
			var validationResult = await _createCartItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw AppException.BadRequest("Validation failed", errors);
			}

			var variant = await _unitOfWork.Variants.GetByIdAsync(request.VariantId) ?? throw AppException.NotFound("Product variant not found");

			variant.EnsureAvailableForCart();

			var existing = await _unitOfWork.CartItems.FirstOrDefaultAsync(
				ci => ci.UserId == userId && ci.VariantId == request.VariantId);

			var totalQuantity = existing != null ? existing.Quantity + request.Quantity : request.Quantity;

			var hasStock = await _stockService.HasSufficientStockAsync(request.VariantId, totalQuantity);
			if (!hasStock)
			{
				throw AppException.BadRequest("Insufficient stock for the requested quantity");
			}

			if (existing != null)
			{
				existing.SetQuantity(totalQuantity);
				_unitOfWork.CartItems.Update(existing);
				var updated = await _unitOfWork.SaveChangesAsync();

				if (!updated)
				{
					throw AppException.Internal("Could not update cart item");
				}

				return BaseResponse<string>.Ok(existing.Id.ToString(), "Cart item quantity updated successfully");
			}

			var cartItem = CartItem.Create(userId, request.VariantId, request.Quantity);

			await _unitOfWork.CartItems.AddAsync(cartItem);
			var saved = await _unitOfWork.SaveChangesAsync();

			if (!saved)
			{
				throw AppException.Internal("Could not add item to cart");
			}

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item added to cart successfully");
		}

		public async Task<BaseResponse<string>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
		{
			var cartItem = await _unitOfWork.CartItems.GetByIdAsync(cartItemId) ?? throw AppException.NotFound("Cart item not found");
			if (!cartItem.IsOwnedBy(userId))
				throw AppException.Forbidden("Cart item does not belong to user");

			_unitOfWork.CartItems.Remove(cartItem);
			var saved = await _unitOfWork.SaveChangesAsync();

			if (!saved)
			{
				throw AppException.Internal("Could not remove item from cart");
			}

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item removed from cart successfully");
		}

		public async Task<BaseResponse<string>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
		{
			var validationResult = await _updateCartItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw AppException.BadRequest("Validation failed", errors);
			}

			var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
					ci => ci.Id == cartItemId && ci.UserId == userId) ?? throw AppException.NotFound("Cart item not found");

			if (request.Quantity == 0)
			{
				_unitOfWork.CartItems.Remove(cartItem);
				var removed = await _unitOfWork.SaveChangesAsync();

				if (!removed)
					throw AppException.Internal("Could not remove cart item");

				return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item removed from cart successfully");
			}

			var hasStock = await _stockService.HasSufficientStockAsync(cartItem.VariantId, request.Quantity);
			if (!hasStock)
			{
				throw AppException.BadRequest("Insufficient stock for the requested quantity");
			}

			cartItem.SetQuantity(request.Quantity);
			_unitOfWork.CartItems.Update(cartItem);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
			{
				throw AppException.Internal("Could not update cart item");
			}

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Cart item updated successfully");
		}
	}
}
