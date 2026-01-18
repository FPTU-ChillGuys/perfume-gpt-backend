using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly ICartItemRepository _cartItemRepository;
        private readonly IValidator<CreateCartRequest> _createCartValidator;

        public CartService(ICartRepository cartRepository, IValidator<CreateCartRequest> createCartValidator, ICartItemRepository cartItemRepository)
        {
            _cartRepository = cartRepository;
            _createCartValidator = createCartValidator;
            _cartItemRepository = cartItemRepository;
        }

        public async Task<Guid> CreateCartAsync(CreateCartRequest request)
        {
            var validationResult = await _createCartValidator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var cart = await _cartRepository.GetByUserIdAsync(request.UserId);
            return cart.Id;
        }

        public async Task<BaseResponse<PagedResult<GetCartItemResponse>>> GetPagedCartItemsAsync(Guid UserId, GetPagedCartItemsRequest request)
        {
            var cart = await _cartRepository.GetByUserIdAsync(UserId);
            if (cart == null)
            {
                return BaseResponse<PagedResult<GetCartItemResponse>>.Fail("Cart not found", ResponseErrorType.NotFound);
            }
            var (items, total) = await _cartItemRepository.GetPagedCartItemsByCartIdAsync(cart.Id, request.PageNumber, request.PageSize);

            var paged = new PagedResult<GetCartItemResponse>(items, request.PageNumber, request.PageSize, total);
            return BaseResponse<PagedResult<GetCartItemResponse>>.Ok(paged);
        }

        public async Task<BaseResponse<GetCartResponse>> GetCartByUserIdAsync(Guid userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
            {
                return BaseResponse<GetCartResponse>.Fail("Cart not found", ResponseErrorType.NotFound);
            }
            var (items, total) = await _cartItemRepository.GetPagedCartItemsByCartIdAsync(cart.Id, 1, int.MaxValue);
            var itemsPaged = new PagedResult<GetCartItemResponse>(items, 1, int.MaxValue, total);
            var totalPrice = await _cartItemRepository.GetCartTotalByCartIdAsync(cart.Id);

            var response = new GetCartResponse
            {
                Items = itemsPaged,
                TotalPrice = totalPrice
            };

            return BaseResponse<GetCartResponse>.Ok(response);
        }

        public async Task<decimal> GetCartTotalAsync(Guid userId, Guid voucherId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
            {
                return 0m;
            }
            var total = await _cartItemRepository.GetCartTotalByCartIdAsync(cart.Id);
            return total;
        }

        //public async Task<int> GetCartItemCountAsync(Guid userId)
        //{
        //    var cart = await _cartRepository.GetByUserIdAsync(userId);
        //    if (cart == null)
        //    {
        //        return 0;
        //    }
        //    var itemCount = await _cartItemRepository.GetCartItemCountByCartIdAsync(cart.Id);
        //    return itemCount;
        //}

        //public async Task<bool> ClearCartAsync(Guid userId)
        //{
        //    var cart = await _cartRepository.GetByUserIdAsync(userId);
        //    if (cart == null)
        //    {
        //        return false;
        //    }
        //    await _cartItemRepository.DeleteCartItemsByCartIdAsync(cart.Id);
        //    return true;
        //}
    }
}
