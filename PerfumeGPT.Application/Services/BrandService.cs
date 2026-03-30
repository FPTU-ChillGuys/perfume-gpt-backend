using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class BrandService : IBrandService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;

		public BrandService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(
				await _unitOfWork.Brands.GetBrandLookupAsync());
		}

		public async Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id)
		{
			var result = await _unitOfWork.Brands.GetBrandByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");
			return BaseResponse<BrandResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<BrandResponse>>> GetAllBrandsAsync()
		{
			var result = await _unitOfWork.Brands.GetAllBrandsAsync();
			return BaseResponse<List<BrandResponse>>.Ok(result);
		}

		public async Task<BaseResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request)
		{
			var normalizedName = Brand.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Brands.AnyAsync(b => b.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Brand name already exists.");

			var entity = Brand.Create(normalizedName);
			await _unitOfWork.Brands.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create brand");

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
		{
			var entity = await _unitOfWork.Brands.GetByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");

			var normalizedName = Brand.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Brands.AnyAsync(b => b.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Brand name already exists.");

			entity.Rename(normalizedName);
			_unitOfWork.Brands.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update brand");

			return BaseResponse<BrandResponse>.Ok(entity.Adapt<BrandResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteBrandAsync(int id)
		{
			var entity = await _unitOfWork.Brands.GetByIdAsync(id)
				?? throw AppException.NotFound("Brand not found");

			var hasProducts = await _unitOfWork.Brands.HasProductsAsync(id);
			if (hasProducts)
				throw AppException.Conflict("Cannot delete brand with associated products.");

			_unitOfWork.Brands.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete brand");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
