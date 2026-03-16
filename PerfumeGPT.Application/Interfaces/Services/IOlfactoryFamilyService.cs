using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.OlfactoryFamilies;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOlfactoryFamilyService
	{
		Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync();
	}
}
