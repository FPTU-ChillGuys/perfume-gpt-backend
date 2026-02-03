using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Variants;
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

		public async Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode)
		{
			return await _context.ProductVariants
				.Where(v => v.Barcode == barcode && !v.IsDeleted)
				.ProjectToType<ProductVariantResponse>()
				.FirstOrDefaultAsync();
		}

		public async Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId)
		{
			return await _context.ProductVariants
				.Where(v => v.Id == variantId && !v.IsDeleted)
				.ProjectToType<ProductVariantResponse>()
				.FirstOrDefaultAsync();
		}

		public async Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(GetPagedVariantsRequest request)
		{
			var query = _context.ProductVariants
				.Where(v => !v.IsDeleted);

			var totalCount = await query.CountAsync();

			var items = await query
				.OrderByDescending(v => v.CreatedAt)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ProjectToType<VariantPagedItem>()
				.ToListAsync();

			return (items, totalCount);
		}
	}
}

