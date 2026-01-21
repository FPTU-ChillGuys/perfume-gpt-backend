using Microsoft.EntityFrameworkCore;
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
	}
}
