using Microsoft.EntityFrameworkCore;
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

		public async Task<List<CartItem>> GetCartItemByCartIdAsync(Guid cartId)
		{
			var items = await _context.CartItems
				.Include(ci => ci.ProductVariant)
					.ThenInclude(pv => pv.Product)
				.Where(ci => ci.CartId == cartId)
				.ToListAsync();

			return items;
		}
	}
}
