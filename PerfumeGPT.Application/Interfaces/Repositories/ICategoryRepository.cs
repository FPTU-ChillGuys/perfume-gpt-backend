using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ICategoryRepository : IGenericRepository<Category>
	{
		Task<List<CategoriesLookupItem>> GetCategoriesLookupItemsAsync();
		Task<List<CategoryResponse>> GetAllCategoriesAsync();
		Task<CategoryResponse?> GetCategoryByIdAsync(int id);
		Task<bool> HasProductsAsync(int categoryId);
	}
}
