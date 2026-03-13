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

		public async Task<List<CategoryResponse>> GetAllCategoriesAsync()
		{
			return await _context.Categories
				.AsNoTracking()
				.ProjectToType<CategoryResponse>()
				.ToListAsync();
		}

		public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
		{
			return await _context.Categories
				.Where(c => c.Id == id)
				.ProjectToType<CategoryResponse>()
				.FirstOrDefaultAsync();
		}
	}
}
