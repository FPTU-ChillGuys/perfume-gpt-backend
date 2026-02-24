using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly ICategoryRepository _categoryRepository;

		public CategoryService(ICategoryRepository categoryRepository)
		{
			_categoryRepository = categoryRepository;
		}

		public async Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync()
		{
			var categories = await _categoryRepository.GetCategoriesLookupItemsAsync();
			return BaseResponse<List<CategoriesLookupItem>>.Ok(categories);
		}
	}
}
