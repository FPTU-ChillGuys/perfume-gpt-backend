using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Concentrations;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IConcentrationService
	{
		Task<BaseResponse<List<ConcentrationLookupDto>>> GetConcentrationLookup();
		Task<BaseResponse<ConcentrationResponse>> GetConcentrationByIdAsync(int id);
		Task<BaseResponse<List<ConcentrationResponse>>> GetAllConcentrationsAsync();
		Task<BaseResponse<ConcentrationResponse>> CreateConcentrationAsync(CreateConcentrationRequest request);
		Task<BaseResponse<ConcentrationResponse>> UpdateConcentrationAsync(int id, UpdateConcentrationRequest request);
		Task<BaseResponse<bool>> DeleteConcentrationAsync(int id);
	}
}
