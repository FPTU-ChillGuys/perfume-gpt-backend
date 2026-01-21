using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CartRepository : GenericRepository<Cart>, ICartRepository
	{
		public CartRepository(PerfumeDbContext context) : base(context)
		{

		}

		public async Task<bool> ClearCartByUserIdAsync(Guid userId)
		{
			var cart = await FirstOrDefaultAsync(c => c.UserId == userId, c => c.Include(c => c.Items));
			if (cart == null)
			{
				return false;
			}
			cart.Items.Clear();
			Update(cart);
			return await SaveChangesAsync();
		}

		public async Task<Cart> GetByUserIdAsync(Guid userId)
		{
			var cart = await FirstOrDefaultAsync(c => c.UserId == userId);
			if (cart == null)
			{
				cart = new Cart
				{
					UserId = userId,
					Items = new List<CartItem>()
				};
				await AddAsync(cart);
				await SaveChangesAsync();
			}
			return cart;
		}
	}
}
