using MapsterMapper;
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
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public BrandService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(
				await _unitOfWork.Brands.GetBrandLookupAsync());
		}

		public async Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id)
		{
			var result = await _unitOfWork.Brands.GetBrandByIdAsync(id)
			  ?? throw AppException.NotFound("Không tìm thấy thương hiệu");
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
				throw AppException.Conflict("Tên thương hiệu đã tồn tại.");

			var entity = Brand.Create(normalizedName);
			await _unitOfWork.Brands.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo thương hiệu thất bại");

			var createdBrand = _mapper.Map<BrandResponse>(entity);
			return BaseResponse<BrandResponse>.Ok(createdBrand);
		}

		public async Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request)
		{
			var entity = await _unitOfWork.Brands.GetByIdAsync(id)
			  ?? throw AppException.NotFound("Không tìm thấy thương hiệu");

			var normalizedName = Brand.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Brands.AnyAsync(b => b.Name.ToUpper() == normalizedName);

			if (exists)
				throw AppException.Conflict("Tên thương hiệu đã tồn tại.");

			entity.Rename(normalizedName);
			_unitOfWork.Brands.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật thương hiệu thất bại");

			var updatedBrand = _mapper.Map<BrandResponse>(entity);
			return BaseResponse<BrandResponse>.Ok(updatedBrand);
		}

		public async Task<BaseResponse<bool>> DeleteBrandAsync(int id)
		{
			var entity = await _unitOfWork.Brands.GetByIdAsync(id)
			  ?? throw AppException.NotFound("Không tìm thấy thương hiệu");

			var hasProducts = await _unitOfWork.Brands.HasProductsAsync(id);
			if (hasProducts)
				throw AppException.Conflict("Không thể xóa thương hiệu có sản phẩm liên kết.");

			_unitOfWork.Brands.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa thương hiệu thất bại");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
