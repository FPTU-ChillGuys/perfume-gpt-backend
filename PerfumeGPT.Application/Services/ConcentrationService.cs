using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ConcentrationService : IConcentrationService
	{
		private readonly IConcentrationRepository _concentrationRepository;
		private readonly IMapper _mapper;

		public ConcentrationService(IConcentrationRepository concentrationRepository, IMapper mapper)
		{
			_concentrationRepository = concentrationRepository;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup()
		{
			return BaseResponse<List<ConcentrationLookupDto>>.Ok(await _concentrationRepository.GetConcentrationLookupsAsync());
		}

		public async Task<BaseResponse<ConcentrationResponse>> GetConcentrationByIdAsync(int id)
		{
			var result = await _concentrationRepository.GetConcentrationByIdAsync(id);
			if (result == null)
				return BaseResponse<ConcentrationResponse>.Fail("Concentration not found", ResponseErrorType.NotFound);
			return BaseResponse<ConcentrationResponse>.Ok(result);
		}

		public async Task<BaseResponse<List<ConcentrationResponse>>> GetAllConcentrationsAsync()
		{
			var result = await _concentrationRepository.GetAllConcentrationsAsync();
			return BaseResponse<List<ConcentrationResponse>>.Ok(result);
		}

		public async Task<BaseResponse<ConcentrationResponse>> CreateConcentrationAsync(CreateConcentrationRequest request)
		{
			var entity = _mapper.Map<Concentration>(request);
			await _concentrationRepository.AddAsync(entity);
			await _concentrationRepository.SaveChangesAsync();
			return BaseResponse<ConcentrationResponse>.Ok(_mapper.Map<ConcentrationResponse>(entity));
		}

		public async Task<BaseResponse<ConcentrationResponse>> UpdateConcentrationAsync(int id, UpdateConcentrationRequest request)
		{
			var entity = await _concentrationRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<ConcentrationResponse>.Fail("Concentration not found", ResponseErrorType.NotFound);

			_mapper.Map(request, entity);
			_concentrationRepository.Update(entity);
			await _concentrationRepository.SaveChangesAsync();

			return BaseResponse<ConcentrationResponse>.Ok(_mapper.Map<ConcentrationResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteConcentrationAsync(int id)
		{
			var entity = await _concentrationRepository.GetByIdAsync(id);
			if (entity == null)
				return BaseResponse<bool>.Fail("Concentration not found", ResponseErrorType.NotFound);

			if (entity.Variants != null && entity.Variants.Any())
				return BaseResponse<bool>.Fail("Cannot delete concentration with associated product variants", ResponseErrorType.Conflict);

			_concentrationRepository.Remove(entity);
			await _concentrationRepository.SaveChangesAsync();

			return BaseResponse<bool>.Ok(true);
		}
	}
}
