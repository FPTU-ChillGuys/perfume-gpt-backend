using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICartItemRepository : IGenericRepository<CartItem>
	{
		Task<List<GetCartItemResponse>> GetCartItemsByUserIdAsync(Guid userId, List<Guid>? itemIds = null);
		Task<List<CartCheckoutItemDto>> GetCartCheckoutItemsAsync(Guid userId, List<Guid>? itemIds = null);
		Task<List<CartItemPriceDto>> GetCartItemPricesAsync(Guid userId, List<Guid>? cartItemIds);
		Task<bool> HasItemsAsync(Guid userId);
		Task ClearCartByUserIdAsync(Guid userId, List<Guid>? itemIds);
	}
}
