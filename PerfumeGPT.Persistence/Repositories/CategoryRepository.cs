using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
	{
		public CategoryRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<CategoriesLookupItem>> GetCategoriesLookupItemsAsync()
		=> await _context.Categories
			.AsNoTracking()
			.Select(c => new CategoriesLookupItem
			{
				Id = c.Id,
				Name = c.Name
			})
			.ToListAsync();

		public async Task<List<CategoryResponse>> GetAllCategoriesAsync()
		=> await _context.Categories
			.AsNoTracking()
			.Select(c => new CategoryResponse
			{
				Id = c.Id,
				Name = c.Name
			})
			.ToListAsync();

		public async Task<CategoryResponse?> GetCategoryByIdAsync(int id)
		=> await _context.Categories
			.Where(c => c.Id == id)
			.Select(c => new CategoryResponse
			{
				Id = c.Id,
				Name = c.Name
			})
			.FirstOrDefaultAsync();

		public async Task<bool> HasProductsAsync(int categoryId)
		=> await _context.Products.AnyAsync(p => p.CategoryId == categoryId);
	}
}
