using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CategoryService : ICategoryService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public CategoryService(ICategoryRepository categoryRepository, IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync()
		{
			var categories = await _unitOfWork.Categories.GetCategoriesLookupItemsAsync();
			return BaseResponse<List<CategoriesLookupItem>>.Ok(categories);
		}

		public async Task<BaseResponse<CategoryResponse>> GetCategoryByIdAsync(int id)
		{
			var result = await _unitOfWork.Categories.GetCategoryByIdAsync(id)
				?? throw AppException.NotFound("Category not found");
			return BaseResponse<CategoryResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<CategoryResponse>>> GetAllCategoriesAsync()
		{
			var result = await _unitOfWork.Categories.GetAllCategoriesAsync();
			return BaseResponse<List<CategoryResponse>>.Ok(result);
		}

		public async Task<BaseResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
		{
			var normalizedName = Category.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Categories.AnyAsync(c => c.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Category name already exists.");

			var entity = Category.Create(normalizedName);
			await _unitOfWork.Categories.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create category");

			return BaseResponse<CategoryResponse>.Ok(_mapper.Map<CategoryResponse>(entity));
		}

		public async Task<BaseResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
		{
			var entity = await _unitOfWork.Categories.GetByIdAsync(id)
				?? throw AppException.NotFound("Category not found");

			var normalizedName = Category.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Categories.AnyAsync(c => c.Id != id && c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Category name already exists.");

			entity.Rename(normalizedName);
			_unitOfWork.Categories.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update category");

			return BaseResponse<CategoryResponse>.Ok(_mapper.Map<CategoryResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteCategoryAsync(int id)
		{
			var entity = await _unitOfWork.Categories.GetByIdAsync(id)
				   ?? throw AppException.NotFound("Category not found");

			var hasProducts = await _unitOfWork.Categories.HasProductsAsync(id);
			if (!hasProducts) throw AppException.Conflict("Cannot delete category with associated products.");

			_unitOfWork.Categories.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete category");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
