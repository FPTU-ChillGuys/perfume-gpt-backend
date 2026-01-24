using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CartItemService : ICartItemService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVariantService _variantService;
		private readonly IStockService _stockService;
		private readonly IValidator<CreateCartItemRequest> _createCartItemValidator;
		private readonly IValidator<UpdateCartItemRequest> _updateCartItemValidator;

		public CartItemService(
			IUnitOfWork unitOfWork,
			IVariantService variantService,
			IStockService stockService,
			IValidator<CreateCartItemRequest> createCartItemValidator,
			IValidator<UpdateCartItemRequest> updateCartItemValidator)
		{
			_unitOfWork = unitOfWork;
			_variantService = variantService;
			_stockService = stockService;
			_createCartItemValidator = createCartItemValidator;
			_updateCartItemValidator = updateCartItemValidator;
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
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var variant = await _unitOfWork.Variants.GetByIdAsync(request.VariantId);
				if (variant == null)
				{
					return BaseResponse<string>.Fail("Product variant not found", ResponseErrorType.NotFound);
				}

				var (IsValid, ErrorMessage) = _variantService.ValidateVariantForCart(variant);
				if (!IsValid)
				{
					return BaseResponse<string>.Fail(ErrorMessage!, ResponseErrorType.BadRequest);
				}

				var existing = await _unitOfWork.CartItems.FirstOrDefaultAsync(
					ci => ci.CartId == cart.Id && ci.VariantId == request.VariantId);

				var totalQuantity = existing != null ? existing.Quantity + request.Quantity : request.Quantity;

				var hasStock = await _stockService.IsValidToCartAsync(request.VariantId, totalQuantity);
				if (!hasStock)
				{
					return BaseResponse<string>.Fail(
						"Insufficient stock for the requested quantity",
						ResponseErrorType.BadRequest);
				}

				if (existing != null)
				{
					existing.Quantity = totalQuantity;
					_unitOfWork.CartItems.Update(existing);
					var updated = await _unitOfWork.SaveChangesAsync();

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

				await _unitOfWork.CartItems.AddAsync(cartItem);
				var saved = await _unitOfWork.SaveChangesAsync();

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
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var cartItem = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);
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

				_unitOfWork.CartItems.Remove(cartItem);
				var saved = await _unitOfWork.SaveChangesAsync();

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
				var cart = await _unitOfWork.Carts.GetByUserIdAsync(userId);
				if (cart == null)
				{
					return BaseResponse<string>.Fail("Cart not found for user", ResponseErrorType.NotFound);
				}

				var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
					ci => ci.Id == cartItemId && ci.CartId == cart.Id);

				if (cartItem == null)
				{
					return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);
				}

				if (request.Quantity <= 0)
				{
					_unitOfWork.CartItems.Remove(cartItem);
					var removed = await _unitOfWork.SaveChangesAsync();

					if (!removed)
					{
						return BaseResponse<string>.Fail(
							"Could not remove cart item",
							ResponseErrorType.InternalError);
					}

					return BaseResponse<string>.Ok(cartItem.Id.ToString(), "Item removed from cart successfully");
				}

				var hasStock = await _stockService.IsValidToCartAsync(cartItem.VariantId, request.Quantity);
				if (!hasStock)
				{
					return BaseResponse<string>.Fail(
						"Insufficient stock for the requested quantity",
						ResponseErrorType.BadRequest);
				}

				cartItem.Quantity = request.Quantity;
				_unitOfWork.CartItems.Update(cartItem);

				var saved = await _unitOfWork.SaveChangesAsync();
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
	}
}
