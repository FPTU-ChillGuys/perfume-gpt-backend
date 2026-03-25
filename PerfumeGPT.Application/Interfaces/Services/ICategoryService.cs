using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICategoryService
	{
		Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync();
		Task<BaseResponse<CategoryResponse>> GetCategoryByIdAsync(int id);
		Task<BaseResponse<List<CategoryResponse>>> GetAllCategoriesAsync();
		Task<BaseResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request);
		Task<BaseResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request);
		Task<BaseResponse<bool>> DeleteCategoryAsync(int id);
	}
}
