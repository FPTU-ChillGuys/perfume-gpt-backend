using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ProductRepository : GenericRepository<Product>, IProductRepository
	{
		public ProductRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<Product?> GetProductWithDetailsAsync(Guid productId)
		{
			return await _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.FragranceFamily)
				.Include(p => p.Media.Where(m => !m.IsDeleted))
				.Include(p => p.Variants)
					.ThenInclude(v => v.Concentration)
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);
		}

		public async Task<(List<Product> Items, int TotalCount)> GetPagedProductsWithDetailsAsync(GetPagedProductRequest request)
		{
			var query = _context.Products
				.Include(p => p.Brand)
				.Include(p => p.Category)
				.Include(p => p.FragranceFamily)
				.Where(p => !p.IsDeleted)
				.AsNoTracking();

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(p => p.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}

