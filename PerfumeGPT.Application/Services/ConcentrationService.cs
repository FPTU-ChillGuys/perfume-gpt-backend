using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class ConcentrationService : IConcentrationService
	{
		private readonly IConcentrationRepository _concentrationRepository;

		public ConcentrationService(IConcentrationRepository concentrationRepository)
		{
			_concentrationRepository = concentrationRepository;
		}

		public async Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup()
		{
			return BaseResponse<List<ConcentrationLookupDto>>.Ok(await _concentrationRepository.GetConcentrationLookupsAsync());
		}
	}
}
