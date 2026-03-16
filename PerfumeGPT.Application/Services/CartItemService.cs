using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CartItemService : ICartItemService
	{
		#region Dependencies

		private readonly IUnitOfWork _unitOfWork;
		private readonly IVariantService _variantService;
		private readonly IStockService _stockService;
		private readonly IValidator<CreateCartItemRequest> _createCartItemValidator;
		private readonly IValidator<UpdateCartItemRequest> _updateCartItemValidator;
		private readonly IMapper _mapper;

		public CartItemService(
			IUnitOfWork unitOfWork,
			IVariantService variantService,
			IStockService stockService,
			IValidator<CreateCartItemRequest> createCartItemValidator,
			IValidator<UpdateCartItemRequest> updateCartItemValidator,
			IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_variantService = variantService;
			_stockService = stockService;
			_createCartItemValidator = createCartItemValidator;
			_updateCartItemValidator = updateCartItemValidator;
			_mapper = mapper;
		}

		#endregion

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
					ci => ci.UserId == userId && ci.VariantId == request.VariantId);

				var totalQuantity = existing != null ? existing.Quantity + request.Quantity : request.Quantity;

				var hasStock = await _stockService.HasSufficientStockAsync(request.VariantId, totalQuantity);
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

				var cartItem = _mapper.Map<CartItem>(request);
				cartItem.UserId = userId;

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
			try
			{
				var cartItem = await _unitOfWork.CartItems.GetByIdAsync(cartItemId);
				if (cartItem == null)
				{
					return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);
				}

				if (cartItem.UserId != userId)
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
				var cartItem = await _unitOfWork.CartItems.FirstOrDefaultAsync(
					ci => ci.Id == cartItemId && ci.UserId == userId);

				if (cartItem == null)
				{
					return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);
				}

				if (request.Quantity == 0)
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

				var hasStock = await _stockService.HasSufficientStockAsync(cartItem.VariantId, request.Quantity);
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
