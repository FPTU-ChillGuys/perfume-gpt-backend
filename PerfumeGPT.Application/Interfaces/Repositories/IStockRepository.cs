using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IStockRepository : IGenericRepository<Stock>
	{
		Task<bool> UpdateStockAsync(Guid variantId);
		Task<bool> IsLowStockAsync(Guid variantId);
		Task<bool> IsValidToCart(Guid variantId, int requiredQuantity);
	}
}
