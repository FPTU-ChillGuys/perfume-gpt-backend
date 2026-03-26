using FluentValidation;
using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BrandService : IBrandService
	{
		#region Dependencies
		private readonly IBrandRepository _brandRepository;
		private readonly IValidator<CreateBrandRequest> _createValidator;
		private readonly IValidator<UpdateBrandRequest> _updateValidator;

		public BrandService(
			IBrandRepository brandRepository,
			IValidator<CreateBrandRequest> createValidator,
			IValidator<UpdateBrandRequest> updateValidator)
		{
			_brandRepository = brandRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(
				await _brandRepository.GetBrandLookupAsync());
		}

		public async Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id)
		{
			var result = await _brandRepository.GetBrandByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");
			return BaseResponse<BrandResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<BrandResponse>>> GetAllBrandsAsync()
		{
			var result = await _brandRepository.GetAllBrandsAsync();
			return BaseResponse<List<BrandResponse>>.Ok(result);
		}

		public async Task<BaseResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var normalizedName = Brand.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _brandRepository.AnyAsync(b => b.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Brand name already exists.");

			var entity = Brand.Create(normalizedName);
			await _brandRepository.AddAsync(entity);

			var saved = await _brandRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create brand");

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = await _brandRepository.GetByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");

			var normalizedName = Brand.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _brandRepository.AnyAsync(b => b.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Brand name already exists.");

			entity.Rename(normalizedName);
			_brandRepository.Update(entity);

			var saved = await _brandRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update brand");

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteBrandAsync(int id)
		{
			var entity = await _brandRepository.GetByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");

			var hasProducts = await _brandRepository.HasProductsAsync(id);
			Brand.EnsureCanBeDeleted(hasProducts);

			_brandRepository.Remove(entity);
			var saved = await _brandRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete brand");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
