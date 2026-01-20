using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class CartItemService : ICartItemService
	{
		private readonly ICartItemRepository _cartItemRepo;
		private readonly ICartRepository _cartRepo;
		private readonly IVariantRepository _variantRepo;
		private readonly IStockRepository _stockRepo;
		private readonly IValidator<CreateCartItemRequest> _createCartItemValidator;
		private readonly IValidator<UpdateCartItemRequest> _updateCartItemValidator;

		public CartItemService(
			ICartItemRepository cartItemRepo,
			IValidator<CreateCartItemRequest> createCartItemValidator,
			IValidator<UpdateCartItemRequest> updateCartItemValidator,
			ICartRepository cartRepo,
			IStockRepository stockRepo,
			IVariantRepository variantRepo)
		{
			_cartItemRepo = cartItemRepo;
			_createCartItemValidator = createCartItemValidator;
			_updateCartItemValidator = updateCartItemValidator;
			_cartRepo = cartRepo;
			_stockRepo = stockRepo;
			_variantRepo = variantRepo;
		}

		public async Task<BaseResponse<string>> AddToCartAsync(Guid userId, CreateCartItemRequest request)
		{
			var validationResult = await _createCartItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			try
			{
				var cart = await _cartRepo.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var variant = await _variantRepo.GetByIdAsync(request.VariantId);
				if (variant == null)
				{
					return BaseResponse<string>.Fail("Product variant not found", ResponseErrorType.NotFound);
				}

				var variantValidation = ValidateVariant(variant);
				if (!variantValidation.IsValid)
				{
					return BaseResponse<string>.Fail(variantValidation.ErrorMessage!, ResponseErrorType.BadRequest);
				}

				var existing = await _cartItemRepo.FirstOrDefaultAsync(
					ci => ci.CartId == cart.Id && ci.VariantId == request.VariantId);

				var totalQuantity = existing != null ? existing.Quantity + request.Quantity : request.Quantity;

				var hasStock = await _stockRepo.IsValidToCart(request.VariantId, totalQuantity);
				if (!hasStock)
				{
					return BaseResponse<string>.Fail(
						"Insufficient stock for the requested quantity",
						ResponseErrorType.BadRequest);
				}

				if (existing != null)
				{
					existing.Quantity = totalQuantity;
					_cartItemRepo.Update(existing);
					var updated = await _cartItemRepo.SaveChangesAsync();

					if (!updated)
					{
						return BaseResponse<string>.Fail(
							"Could not update cart item",
							ResponseErrorType.InternalError);
					}

					return BaseResponse<string>.Ok(existing.Id.ToString(), "Cart item quantity updated successfully");
				}

				var cartItem = new CartItem
				{
					CartId = cart.Id,
					VariantId = request.VariantId,
					Quantity = request.Quantity
				};

				await _cartItemRepo.AddAsync(cartItem);
				var saved = await _cartItemRepo.SaveChangesAsync();

				if (!saved)
				{
					return BaseResponse<string>.Fail(
						"Could not add item to cart",
						ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item added to cart successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error adding item to cart: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
		{
			if (cartItemId == Guid.Empty)
			{
				return BaseResponse<string>.Fail("Cart item ID is required", ResponseErrorType.BadRequest);
			}

			try
			{
				var cart = await _cartRepo.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var cartItem = await _cartItemRepo.GetByIdAsync(cartItemId);
				if (cartItem == null)
				{
					return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);
				}

				if (cartItem.CartId != cart.Id)
				{
					return BaseResponse<string>.Fail(
						"Cart item does not belong to user",
						ResponseErrorType.Forbidden);
				}

				_cartItemRepo.Remove(cartItem);
				var saved = await _cartItemRepo.SaveChangesAsync();

				if (!saved)
				{
					return BaseResponse<string>.Fail(
						"Could not remove item from cart",
						ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item removed from cart successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error removing item from cart: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> UpdateCartItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
		{
			var validationResult = await _updateCartItemValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			try
			{
				var cart = await _cartRepo.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var cartItem = await _cartItemRepo.FirstOrDefaultAsync(
					ci => ci.Id == cartItemId && ci.CartId == cart.Id);

				if (cartItem == null)
				{
					return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);
				}

			if (request.Quantity <= 0)
			{
				_cartItemRepo.Remove(cartItem);
				var removed = await _cartItemRepo.SaveChangesAsync();

				if (!removed)
				{
					return BaseResponse<string>.Fail(
						"Could not remove cart item",
						ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item removed from cart successfully");
			}

			var hasStock = await _stockRepo.IsValidToCart(cartItem.VariantId, request.Quantity);
			if (!hasStock)
			{
				return BaseResponse<string>.Fail(
					"Insufficient stock for the requested quantity",
					ResponseErrorType.BadRequest);
			}

			cartItem.Quantity = request.Quantity;
			_cartItemRepo.Update(cartItem);

			var saved = await _cartItemRepo.SaveChangesAsync();
			if (!saved)
			{
				return BaseResponse<string>.Fail(
					"Could not update cart item",
					ResponseErrorType.InternalError);
			}

			return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Cart item updated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail(
					$"Error updating cart item: {ex.Message}",
					ResponseErrorType.InternalError);
			}
		}

		private static (bool IsValid, string? ErrorMessage) ValidateVariant(ProductVariant variant)
		{
			if (variant.IsDeleted)
			{
				return (false, "This product variant is no longer available");
			}

			if (variant.Status == VariantStatus.Discontinued)
			{
				return (false, "This product variant has been discontinued");
			}

			if (variant.Status == VariantStatus.Inactive)
			{
				return (false, "This product variant is currently inactive");
			}

			if (variant.Status == VariantStatus.out_of_stock)
			{
				return (false, "This product variant is out of stock");
			}

			return (true, null);
		}
	}
}
