using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
	{
		public CategoryRepository(PerfumeDbContext context) : base(context)
		{ }

		public async Task<List<CategoriesLookupItem>> GetCategoriesLookupItemsAsync()
		{
			return await _context.Categories
				.AsNoTracking()
				.ProjectToType<CategoriesLookupItem>()
				.ToListAsync();
		}
	}
}
