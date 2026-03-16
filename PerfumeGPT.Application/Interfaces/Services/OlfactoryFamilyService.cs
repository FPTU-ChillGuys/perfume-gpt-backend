using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;
using PerfumeGPT.Application.Interfaces.Repositories;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public class OlfactoryFamilyService : IOlfactoryFamilyService
	{
		private readonly IOlfactoryFamilyRepository _olfactoryFamilyRepository;

		public OlfactoryFamilyService(IOlfactoryFamilyRepository olfactoryFamilyRepository)
		{
			_olfactoryFamilyRepository = olfactoryFamilyRepository;
		}

		public async Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync()
		{
			return BaseResponse<List<OlfactoryLookupResponse>>.Ok(
				await _olfactoryFamilyRepository.GetOlfactoryFamilyLookupListAsync()
			);
		}
	}
}
