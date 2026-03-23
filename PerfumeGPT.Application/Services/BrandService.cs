using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Brands;
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
		private readonly IMapper _mapper;

		public BrandService(
			 IBrandRepository brandRepository,
			 IValidator<CreateBrandRequest> createValidator,
			 IValidator<UpdateBrandRequest> updateValidator,
			 IMapper mapper)
		{
			_brandRepository = brandRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(await _brandRepository.GetBrandLookupAsync());
		}

		public async Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id)
		{
			var result = await _brandRepository.GetBrandByIdAsync(id);
			return result == null
				? throw new AppException("Brand not found", ResponseErrorType.NotFound)
				: BaseResponse<BrandResponse>.Ok(result);
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
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw new AppException("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			var normalizedName = Brand.NormalizeName(request.Name);

			var exists = await _brandRepository.AnyAsync(b =>
			  b.Name.Equals(normalizedName, StringComparison.CurrentCultureIgnoreCase));

			if (exists)
				throw new AppException("Brand name already exists.", ResponseErrorType.Conflict);

			var entity = _mapper.Map<Brand>(request);
			entity.Rename(normalizedName);

			await _brandRepository.AddAsync(entity);
			await _brandRepository.SaveChangesAsync();
			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				throw new AppException("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			var normalizedName = Brand.NormalizeName(request.Name);

			var entity = await _brandRepository.GetByIdAsync(id) ?? throw new AppException("Brand not found", ResponseErrorType.NotFound);

			var exists = await _brandRepository.AnyAsync(b =>
			  b.Id != id && b.Name.Equals(normalizedName, StringComparison.CurrentCultureIgnoreCase));

			if (exists)
				throw new AppException("Brand name already exists.", ResponseErrorType.Conflict);

			_mapper.Map(request, entity);
			entity.Rename(normalizedName);

			_brandRepository.Update(entity);
			await _brandRepository.SaveChangesAsync();

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteBrandAsync(int id)
		{
			var entity = await _brandRepository.FirstOrDefaultAsync(
				 b => b.Id == id,
				 include: q => q.Include(b => b.Products)) ?? throw new AppException("Brand not found", ResponseErrorType.NotFound);

			entity.EnsureCanBeDeleted();

			_brandRepository.Remove(entity);
			await _brandRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
