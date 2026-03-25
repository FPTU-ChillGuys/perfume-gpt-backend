using FluentValidation;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class CategoryService : ICategoryService
	{
		#region Dependencies
		private readonly ICategoryRepository _categoryRepository;
		private readonly IValidator<CreateCategoryRequest> _createValidator;
		private readonly IValidator<UpdateCategoryRequest> _updateValidator;

		public CategoryService(ICategoryRepository categoryRepository, IValidator<CreateCategoryRequest> createValidator, IValidator<UpdateCategoryRequest> updateValidator)
		{
			_categoryRepository = categoryRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<CategoriesLookupItem>>> GetCategoryLookupAsync()
		{
			var categories = await _categoryRepository.GetCategoriesLookupItemsAsync();
			return BaseResponse<List<CategoriesLookupItem>>.Ok(categories);
		}

		public async Task<BaseResponse<CategoryResponse>> GetCategoryByIdAsync(int id)
		{
			var result = await _categoryRepository.GetCategoryByIdAsync(id)
				?? throw AppException.NotFound("Category not found");
			return BaseResponse<CategoryResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<CategoryResponse>>> GetAllCategoriesAsync()
		{
			var result = await _categoryRepository.GetAllCategoriesAsync();
			return BaseResponse<List<CategoryResponse>>.Ok(result);
		}

		public async Task<BaseResponse<CategoryResponse>> CreateCategoryAsync(CreateCategoryRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = Category.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _categoryRepository.AnyAsync(c => c.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Category name already exists.");

			var entity = Category.Create(normalizedName);
			await _categoryRepository.AddAsync(entity);

			var saved = await _categoryRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create category");

			return BaseResponse<CategoryResponse>.Ok(entity.Adapt<CategoryResponse>());
		}

		public async Task<BaseResponse<CategoryResponse>> UpdateCategoryAsync(int id, UpdateCategoryRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = await _categoryRepository.GetByIdAsync(id)
				?? throw AppException.NotFound("Category not found");

			var normalizedName = Category.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _categoryRepository.AnyAsync(c => c.Id != id && c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Category name already exists.");

			entity.Rename(normalizedName);
			_categoryRepository.Update(entity);

			var saved = await _categoryRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update category");

			return BaseResponse<CategoryResponse>.Ok(entity.Adapt<CategoryResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteCategoryAsync(int id)
		{
			var entity = await _categoryRepository.GetByIdAsync(id)
				   ?? throw AppException.NotFound("Category not found");

			var hasProducts = await _categoryRepository.HasProductsAsync(id);
			Category.EnsureCanBeDeleted(hasProducts);

			_categoryRepository.Remove(entity);
			var saved = await _categoryRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete category");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
