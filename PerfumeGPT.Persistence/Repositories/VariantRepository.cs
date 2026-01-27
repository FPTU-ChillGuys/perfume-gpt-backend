using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class VariantRepository : GenericRepository<ProductVariant>, IVariantRepository
	{
		public VariantRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<ProductVariant?> GetByBarcodeAsync(string barcode)
		{
			return await _context.ProductVariants
				.Include(v => v.Product)
				.Include(v => v.Concentration)
				.FirstOrDefaultAsync(v => v.Barcode == barcode && !v.IsDeleted);
		}

		public async Task<ProductVariant?> GetVariantWithDetailsAsync(Guid variantId)
		{
			return await _context.ProductVariants
				.Include(v => v.Concentration)
				.Include(v => v.Product)
				.AsNoTracking()
				.FirstOrDefaultAsync(v => v.Id == variantId && !v.IsDeleted);
		}

		public async Task<(List<ProductVariant> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(GetPagedVariantsRequest request)
		{
			var query = _context.ProductVariants
				.Include(v => v.Concentration)
				.Where(v => !v.IsDeleted)
				.AsNoTracking();

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}
	}
}

