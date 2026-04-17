using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ConcentrationService : IConcentrationService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public ConcentrationService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup()
		{
			return BaseResponse<List<ConcentrationLookupDto>>.Ok(await _unitOfWork.Concentrations.GetConcentrationLookupsAsync());
		}

		public async Task<BaseResponse<ConcentrationResponse>> GetConcentrationByIdAsync(int id)
		{
			var result = await _unitOfWork.Concentrations.GetConcentrationByIdAsync(id)
				  ?? throw AppException.NotFound("Không tìm thấy nồng độ");

			return BaseResponse<ConcentrationResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<ConcentrationResponse>>> GetAllConcentrationsAsync()
		{
			var result = await _unitOfWork.Concentrations.GetAllConcentrationsAsync();
			return BaseResponse<List<ConcentrationResponse>>.Ok(result);
		}

		public async Task<BaseResponse<ConcentrationResponse>> CreateConcentrationAsync(CreateConcentrationRequest request)
		{
			var normalizedName = Concentration.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Concentrations.AnyAsync(c => c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Tên nồng độ đã tồn tại.");

			var entity = Concentration.Create(normalizedName);
			await _unitOfWork.Concentrations.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo nồng độ thất bại");

			return BaseResponse<ConcentrationResponse>.Ok(_mapper.Map<ConcentrationResponse>(entity));
		}

		public async Task<BaseResponse<ConcentrationResponse>> UpdateConcentrationAsync(int id, UpdateConcentrationRequest request)
		{
			var entity = await _unitOfWork.Concentrations.GetByIdAsync(id)
			  ?? throw AppException.NotFound("Không tìm thấy nồng độ");

			var normalizedName = Concentration.NormalizeName(request.Name).ToUpperInvariant();
			var exists = await _unitOfWork.Concentrations.AnyAsync(c => c.Id != id && c.Name.ToUpper() == normalizedName);
			if (exists)
				throw AppException.Conflict("Tên nồng độ đã tồn tại.");

			entity.Rename(normalizedName);
			_unitOfWork.Concentrations.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật nồng độ thất bại");

			return BaseResponse<ConcentrationResponse>.Ok(_mapper.Map<ConcentrationResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteConcentrationAsync(int id)
		{
			var entity = await _unitOfWork.Concentrations.GetByIdAsync(id)
			  ?? throw AppException.NotFound("Không tìm thấy nồng độ");

			var hasVariants = await _unitOfWork.Concentrations.HasVariantsAsync(id);
			if (!hasVariants) throw AppException.Conflict("Không thể xóa nồng độ có biến thể liên kết.");

			_unitOfWork.Concentrations.Remove(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa nồng độ thất bại");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
