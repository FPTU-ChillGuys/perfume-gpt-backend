using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Requests.GHNs.Address;
using PerfumeGPT.Application.DTOs.Responses.GHNs;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IGHNService
	{
		Task<CalculateFeeResponse?> CalculateShippingFeeAsync(CalculateFeeRequest request);
		Task<List<ProvinceResponse>> GetProvincesAsync();
		Task<List<DistrictResponse>> GetDistrictsByProvinceIdAsync(int provinceId);
		Task<List<WardResponse>> GetWardsByDistrictIdAsync(int districtId);
	}
}
