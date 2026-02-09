using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IProductAttributeRepository : IGenericRepository<ProductAttribute>
	{
		Task<List<ProductAttribute>> GetByProductIdAsync(Guid productId);
		Task<List<ProductAttribute>> GetByVariantIdAsync(Guid variantId);
	}
}
