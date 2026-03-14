using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICartItemRepository : IGenericRepository<CartItem>
	{
		Task<List<GetCartItemResponse>> GetCartItemsByCartIdAsync(Guid cartId, List<Guid>? itemIds = null);
		Task<List<CartCheckoutItemDto>> GetCartCheckoutItemsAsync(Guid cartId, List<Guid>? itemIds = null);
		Task<List<CartItemPriceDto>> GetCartItemPricesAsync(Guid cartId, List<Guid>? cartItemIds);
		Task<bool> HasItemsInCartAsync(Guid cartId);
	}
}
