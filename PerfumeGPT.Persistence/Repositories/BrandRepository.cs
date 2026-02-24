using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class BrandRepository : GenericRepository<Brand>, IBrandRepository
	{
		public BrandRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<BrandLookupItem>> GetBrandLookupAsync()
		{
			return await _context.Brands
				.ProjectToType<BrandLookupItem>()
				.AsNoTracking()
				.ToListAsync();
		}
	}
}
