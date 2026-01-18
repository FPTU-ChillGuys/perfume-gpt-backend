using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
    public class CartItemService : ICartItemService
    {
        private readonly ICartItemRepository _cartItemRepo;
        private readonly ICartRepository _cartRepo;
        private readonly IValidator<CreateCartItemRequest> _createCartItemValidator;
        private readonly IValidator<UpdateCartItemRequest> _updateCartItemValidator;

        public CartItemService(ICartItemRepository cartItemRepo, IValidator<CreateCartItemRequest> createCartItemValidator, IValidator<UpdateCartItemRequest> updateCartItemValidator, ICartRepository cartRepo)
        {
            _cartItemRepo = cartItemRepo;
            _createCartItemValidator = createCartItemValidator;
            _updateCartItemValidator = updateCartItemValidator;
            _cartRepo = cartRepo;
        }

        public Task<BaseResponse<string>> AddToCartAsync(CreateCartItemRequest request)
        {
            return AddToCartInternalAsync(request);
        }

        public Task<BaseResponse<string>> RemoveFromCartAsync(Guid userId, Guid cartItemId)
        {
            return RemoveFromCartInternalAsync(userId, cartItemId);
        }

        public Task<BaseResponse<string>> UpdateCart(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
        {
            return UpdateCartInternalAsync(userId, cartItemId, request);
        }

        private async Task<BaseResponse<string>> AddToCartInternalAsync(CreateCartItemRequest request)
        {
            var validationResult = await _createCartItemValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
            }

            try
            {
                var existing = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.CartId == request.CartId && ci.VariantId == request.VariantId);
                if (existing != null)
                {
                    existing.Quantity += request.Quantity;
                    _cartItemRepo.Update(existing);
                    var ok = await _cartItemRepo.SaveChangesAsync();
                    if (!ok) return BaseResponse<string>.Fail("Could not update cart item", ResponseErrorType.InternalError);
                    return BaseResponse<string>.Ok(existing.Id.ToString(), "Cart updated");
                }

                var entity = new CartItem
                {
                    CartId = request.CartId,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity
                };

                await _cartItemRepo.AddAsync(entity);
                var saved = await _cartItemRepo.SaveChangesAsync();
                if (!saved) return BaseResponse<string>.Fail("Could not add to cart", ResponseErrorType.InternalError);

                return BaseResponse<string>.Ok(entity.Id.ToString(), "Added to cart");
            }
            catch (Exception ex)
            {
                return BaseResponse<string>.Fail(ex.Message, ResponseErrorType.InternalError);
            }
        }

        private async Task<BaseResponse<string>> UpdateCartInternalAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
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
                var existing = await _cartItemRepo.FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.VariantId == cartItemId);
                if (existing == null)
                    return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);

                existing.Quantity = request.Quantity;
                _cartItemRepo.Update(existing);
                var ok = await _cartItemRepo.SaveChangesAsync();
                if (!ok) return BaseResponse<string>.Fail("Could not update cart item", ResponseErrorType.InternalError);

                return BaseResponse<string>.Ok(existing.Id.ToString(), "Cart updated");
            }
            catch (Exception ex)
            {
                return BaseResponse<string>.Fail(ex.Message, ResponseErrorType.InternalError);
            }
        }

        private async Task<BaseResponse<string>> RemoveFromCartInternalAsync(Guid UserId, Guid cartItemId)
        {
            if (cartItemId == Guid.Empty) return BaseResponse<string>.Fail("cartItemId is required", ResponseErrorType.BadRequest);

            try
            {
                var cart = await _cartRepo.GetByUserIdAsync(UserId);
                var existing = await _cartItemRepo.GetByIdAsync(cartItemId);
                if (existing == null || existing.CartId != cart.Id)
                    return BaseResponse<string>.Fail("Cart item not found", ResponseErrorType.NotFound);

                _cartItemRepo.Remove(existing);
                var ok = await _cartItemRepo.SaveChangesAsync();
                if (!ok) return BaseResponse<string>.Fail("Could not remove cart item", ResponseErrorType.InternalError);

                return BaseResponse<string>.Ok(existing.Id.ToString(), "Removed from cart");
            }
            catch (Exception ex)
            {
                return BaseResponse<string>.Fail(ex.Message, ResponseErrorType.InternalError);
            }
        }
    }
}
