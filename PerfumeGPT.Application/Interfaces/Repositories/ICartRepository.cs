using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICartRepository : IGenericRepository<Cart>
	{
		Task<Cart> GetByUserIdAsync(Guid userId);
		Task<bool> ClearCartByUserIdAsync(Guid userId);
	}
}
