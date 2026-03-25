using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using Mapster;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class OlfactoryFamilyService : IOlfactoryFamilyService
	{
		private readonly IOlfactoryFamilyRepository _olfactoryFamilyRepository;
		private readonly IMapper _mapper;

		public OlfactoryFamilyService(IOlfactoryFamilyRepository olfactoryFamilyRepository, IMapper mapper)
		{
			_olfactoryFamilyRepository = olfactoryFamilyRepository;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync()
		{
			return BaseResponse<List<OlfactoryLookupResponse>>.Ok(
				await _olfactoryFamilyRepository.GetOlfactoryFamilyLookupListAsync()
			);
		}

		public async Task<BaseResponse<List<OlfactoryFamilyResponse>>> GetAllOlfactoryFamiliesAsync()
		{
			var result = await _olfactoryFamilyRepository.GetAllOlfactoryFamiliesAsync();
			return BaseResponse<List<OlfactoryFamilyResponse>>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> GetOlfactoryFamilyByIdAsync(int id)
		{
			var result = await _olfactoryFamilyRepository.GetOlfactoryFamilyByIdAsync(id);
			if (result == null)
				return BaseResponse<OlfactoryFamilyResponse>.Fail("OlfactoryFamily not found", ResponseErrorType.NotFound);
			return BaseResponse<OlfactoryFamilyResponse>.Ok(result);
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> CreateOlfactoryFamilyAsync(CreateOlfactoryFamilyRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _olfactoryFamilyRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower());
			if (exists)
				return BaseResponse<OlfactoryFamilyResponse>.Fail("OlfactoryFamily name already exists.", ResponseErrorType.Conflict);

			var entity = _mapper.Map<OlfactoryFamily>(request);
			entity.Name = normalizedName;

			await _olfactoryFamilyRepository.AddAsync(entity);
			await _olfactoryFamilyRepository.SaveChangesAsync();
			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<OlfactoryFamilyResponse>> UpdateOlfactoryFamilyAsync(int id, UpdateOlfactoryFamilyRequest request)
		{
			var normalizedName = request.Name.Trim();

			var exists = await _olfactoryFamilyRepository.AnyAsync(s => s.Name.ToLower() == normalizedName.ToLower() && s.Id != id);
			if (exists)
				return BaseResponse<OlfactoryFamilyResponse>.Fail("OlfactoryFamily name already exists.", ResponseErrorType.Conflict);

			var entity = await _olfactoryFamilyRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<OlfactoryFamilyResponse>.Fail("OlfactoryFamily not found", ResponseErrorType.NotFound);

			_mapper.Map(request, entity);
			entity.Name = normalizedName;

			_olfactoryFamilyRepository.Update(entity);
			await _olfactoryFamilyRepository.SaveChangesAsync();

			return BaseResponse<OlfactoryFamilyResponse>.Ok(entity.Adapt<OlfactoryFamilyResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteOlfactoryFamilyAsync(int id)
		{
			var entity = await _olfactoryFamilyRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<bool>.Fail("OlfactoryFamily not found", ResponseErrorType.NotFound);

			if (entity.ProductFamilyMaps.Count != 0 || entity.CustomerFamilyPreferences.Count != 0)
				return BaseResponse<bool>.Fail("Cannot delete OlfactoryFamily with existing associations.", ResponseErrorType.BadRequest);

			_olfactoryFamilyRepository.Remove(entity);
			await _olfactoryFamilyRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
