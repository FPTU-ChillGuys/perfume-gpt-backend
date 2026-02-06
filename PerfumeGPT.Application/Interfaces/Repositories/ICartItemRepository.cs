using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICartItemRepository : IGenericRepository<CartItem>
	{
		Task<List<GetCartItemResponse>> GetCartItemsByCartIdAsync(Guid cartId);
		Task<List<CartCheckoutItemDto>> GetCartCheckoutItemsAsync(Guid cartId);
		Task<List<CartItemPriceDto>> GetCartItemPricesAsync(Guid cartId);

		Task<bool> HasItemsInCartAsync(Guid cartId);
	}
}
