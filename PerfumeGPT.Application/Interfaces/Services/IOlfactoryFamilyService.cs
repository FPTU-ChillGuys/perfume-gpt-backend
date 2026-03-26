using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.OlfactoryFamilies;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IOlfactoryFamilyService
	{
		Task<BaseResponse<List<OlfactoryLookupResponse>>> GetOlfactoryFamilyLookupListAsync();
		Task<BaseResponse<List<OlfactoryFamilyResponse>>> GetAllOlfactoryFamiliesAsync();
		Task<BaseResponse<OlfactoryFamilyResponse>> GetOlfactoryFamilyByIdAsync(int id);
		Task<BaseResponse<OlfactoryFamilyResponse>> CreateOlfactoryFamilyAsync(CreateOlfactoryFamilyRequest request);
		Task<BaseResponse<OlfactoryFamilyResponse>> UpdateOlfactoryFamilyAsync(int id, UpdateOlfactoryFamilyRequest request);
		Task<BaseResponse<bool>> DeleteOlfactoryFamilyAsync(int id);
	}
}
