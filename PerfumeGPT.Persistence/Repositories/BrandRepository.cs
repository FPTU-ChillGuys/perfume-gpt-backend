using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class BrandRepository : GenericRepository<Brand>, IBrandRepository
	{
		public BrandRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<BrandLookupItem>> GetBrandLookupAsync()
			=> await _context.Brands
				.AsNoTracking()
				.ProjectToType<BrandLookupItem>()
				.ToListAsync();

		public async Task<List<BrandResponse>> GetAllBrandsAsync()
			=> await _context.Brands
				.AsNoTracking()
				.ProjectToType<BrandResponse>()
				.ToListAsync();

		public async Task<BrandResponse?> GetBrandByIdAsync(int id)
			=> await _context.Brands
				.ProjectToType<BrandResponse>()
				.FirstOrDefaultAsync(b => b.Id == id);

		public async Task<bool> HasProductsAsync(int brandId)
			=> await _context.Products.AnyAsync(p => p.BrandId == brandId);
	}
}
