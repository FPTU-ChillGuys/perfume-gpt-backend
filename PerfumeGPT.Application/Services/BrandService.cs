using Mapster;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BrandService : IBrandService
	{
		private readonly IBrandRepository _brandRepository;
		private readonly IMapper _mapper;

		public BrandService(IBrandRepository brandRepository, IMapper mapper)
		{
			_brandRepository = brandRepository;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(await _brandRepository.GetBrandLookupAsync());
		}

		public async Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id)
		{
			var result = await _brandRepository.GetBrandByIdAsync(id);
			if (result == null)
				return BaseResponse<BrandResponse>.Fail("Brand not found", ResponseErrorType.NotFound);
			return BaseResponse<BrandResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<BrandResponse>>> GetAllBrandsAsync()
		{
			var result = await _brandRepository.GetAllBrandsAsync();
			return BaseResponse<List<BrandResponse>>.Ok(result);
		}

		public async Task<BaseResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _brandRepository.AnyAsync(b =>
				b.Name.ToLower() == normalizedName.ToLower());

			if (exists)
				return BaseResponse<BrandResponse>.Fail(
					"Brand name already exists.",
					ResponseErrorType.Conflict);

			var entity = _mapper.Map<Brand>(request);
			entity.Name = normalizedName;

			await _brandRepository.AddAsync(entity);
			await _brandRepository.SaveChangesAsync();
			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _brandRepository.AnyAsync(b =>
				b.Name.ToLower() == normalizedName.ToLower());

			if (exists)
				return BaseResponse<BrandResponse>.Fail(
					"Brand name already exists.",
					ResponseErrorType.Conflict);

			var entity = await _brandRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<BrandResponse>.Fail("Brand not found", ResponseErrorType.NotFound);

			_mapper.Map(request, entity);
			entity.Name = normalizedName;

			_brandRepository.Update(entity);
			await _brandRepository.SaveChangesAsync();

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteBrandAsync(int id)
		{
			var entity = await _brandRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<bool>.Fail("Brand not found", ResponseErrorType.NotFound);

			if (entity.Products != null && entity.Products.Any())
				return BaseResponse<bool>.Fail("Cannot delete brand with associated products", ResponseErrorType.BadRequest);

			_brandRepository.Remove(entity);
			await _brandRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
