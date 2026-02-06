using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CartItemRepository : GenericRepository<CartItem>, ICartItemRepository
	{
		public CartItemRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<GetCartItemResponse>> GetCartItemsByCartIdAsync(Guid cartId)
		{
			var items = await _context.CartItems
				.Where(ci => ci.CartId == cartId)
				.ProjectToType<GetCartItemResponse>()
				.ToListAsync();

			return items;
		}

		public async Task<List<CartCheckoutItemDto>> GetCartCheckoutItemsAsync(Guid cartId)
		{
			var items = await _context.CartItems
				.Where(ci => ci.CartId == cartId)
				.Select(ci => new CartCheckoutItemDto
				{
					VariantId = ci.VariantId,
					Quantity = ci.Quantity
				})
				.ToListAsync();

			return items;
		}

		public async Task<List<CartItemPriceDto>> GetCartItemPricesAsync(Guid cartId)
		{
			var items = await _context.CartItems
				.Where(ci => ci.CartId == cartId)
				.ProjectToType<CartItemPriceDto>()
				.ToListAsync();

			return items;
		}

		public async Task<bool> HasItemsInCartAsync(Guid cartId)
		{
			return await _context.CartItems
				.AnyAsync(ci => ci.CartId == cartId);
		}
	}
}
