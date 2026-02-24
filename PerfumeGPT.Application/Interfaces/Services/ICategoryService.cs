using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Categories;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ICategoryService
	{
		Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync();
	}
}
