using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Domain.Entities;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

namespace PerfumeGPT.Application.Services
{
	public class OlfactoryFamilyService : IOlfactoryFamilyService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public OlfactoryFamilyService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync()
		{
			return BaseResponse<List<OlfactoryLookupResponse>>.Ok(
				await _unitOfWork.OlfactoryFamilies.GetOlfactoryFamilyLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<OlfactoryFamilyResponse>>> GetAllOlfactoryFamiliesAsync()
		{
			var result = await _unitOfWork.OlfactoryFamilies.GetAllOlfactoryFamiliesAsync();
			return BaseResponse<List<OlfactoryFamilyResponse>>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> GetOlfactoryFamilyByIdAsync(int id)
		{
			var result = await _unitOfWork.OlfactoryFamilies.GetOlfactoryFamilyByIdAsync(id);
			return result == null
			  ? throw AppException.NotFound("Không tìm thấy nhóm hương")
				: BaseResponse<OlfactoryFamilyResponse>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> CreateOlfactoryFamilyAsync(CreateOlfactoryFamilyRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _unitOfWork.OlfactoryFamilies.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				throw AppException.Conflict("Tên nhóm hương đã tồn tại.");

			var entity = OlfactoryFamily.Create(request.Name);

			await _unitOfWork.OlfactoryFamilies.AddAsync(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể tạo nhóm hương.");
			return BaseResponse<OlfactoryFamilyResponse>.Ok(_mapper.Map<OlfactoryFamilyResponse>(entity));
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> UpdateOlfactoryFamilyAsync(int id, UpdateOlfactoryFamilyRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _unitOfWork.OlfactoryFamilies.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				throw AppException.Conflict("Tên nhóm hương đã tồn tại.");

			var entity = await _unitOfWork.OlfactoryFamilies.GetByIdAsync(id) ?? throw AppException.NotFound("Không tìm thấy nhóm hương");
			entity.Rename(request.Name);

			_unitOfWork.OlfactoryFamilies.Update(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể cập nhật nhóm hương.");

			return BaseResponse<OlfactoryFamilyResponse>.Ok(_mapper.Map<OlfactoryFamilyResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteOlfactoryFamilyAsync(int id)
		{
			var entity = await _unitOfWork.OlfactoryFamilies.GetByIdAsync(id) ?? throw AppException.NotFound("Không tìm thấy nhóm hương");
			var hasAssociations = await _unitOfWork.OlfactoryFamilies.HasAssociationsAsync(id);
			if (!hasAssociations) throw AppException.Conflict("Không thể xóa nhóm hương có dữ liệu liên kết.");

			_unitOfWork.OlfactoryFamilies.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể xóa nhóm hương.");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
