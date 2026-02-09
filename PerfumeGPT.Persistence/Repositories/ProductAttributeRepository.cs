using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ProductAttributeRepository : GenericRepository<ProductAttribute>, IProductAttributeRepository
	{
		public ProductAttributeRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<ProductAttribute>> GetByProductIdAsync(Guid productId)
		{
			return await _context.ProductAttributes.Where(pa => pa.ProductId == productId).ToListAsync();
		}

		public async Task<List<ProductAttribute>> GetByVariantIdAsync(Guid variantId)
		{
			return await _context.ProductAttributes.Where(pa => pa.VariantId == variantId).ToListAsync();
		}
	}
}
