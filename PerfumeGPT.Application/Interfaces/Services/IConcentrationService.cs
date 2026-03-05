using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Concentrations;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IConcentrationService
	{
		Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup();
	}
}
