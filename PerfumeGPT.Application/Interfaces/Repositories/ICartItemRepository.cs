using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICartItemRepository : IGenericRepository<CartItem>
	{
		Task<List<CartItem>> GetCartItemByCartIdAsync(Guid cartId);

	}
}
