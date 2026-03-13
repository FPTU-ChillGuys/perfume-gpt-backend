using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Categories;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly ICategoryRepository _categoryRepository;
		private readonly IMapper _mapper;

		public CategoryService(ICategoryRepository categoryRepository, IMapper mapper)
		{
			_categoryRepository = categoryRepository;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync()
		{
			var categories = await _categoryRepository.GetCategoriesLookupItemsAsync();
			return BaseResponse<List<CategoriesLookupItem>>.Ok(categories);
		}

		public async Task<BaseResponse<CategoryResponse>> GetCategoryByIdAsync(int id)
		{
			var result = await _categoryRepository.GetCategoryByIdAsync(id);
			if (result == null)
				return BaseResponse<CategoryResponse>.Fail("Category not found", ResponseErrorType.NotFound);
			return BaseResponse<CategoryResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<CategoryResponse>>> GetAllCategoriesAsync()
		{
			var result = await _categoryRepository.GetAllCategoriesAsync();
			return BaseResponse<List<CategoryResponse>>.Ok(result);
		}

		public async Task<BaseResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
		{
			var entity = _mapper.Map<Category>(request);
			await _categoryRepository.AddAsync(entity);
			await _categoryRepository.SaveChangesAsync();
			return BaseResponse<CategoryResponse>.Ok(_mapper.Map<CategoryResponse>(entity));
		}

		public async Task<BaseResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
		{
			var entity = await _categoryRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<CategoryResponse>.Fail("Category not found", ResponseErrorType.NotFound);

			_mapper.Map(request, entity);
			_categoryRepository.Update(entity);
			await _categoryRepository.SaveChangesAsync();

			return BaseResponse<CategoryResponse>.Ok(_mapper.Map<CategoryResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteCategoryAsync(int id)
		{
			var entity = await _categoryRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<bool>.Fail("Category not found", ResponseErrorType.NotFound);

			if (entity.Products != null && entity.Products.Any())
				return BaseResponse<bool>.Fail("Cannot delete category with associated products", ResponseErrorType.BadRequest);

			_categoryRepository.Remove(entity);
			await _categoryRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
