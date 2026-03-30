using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.GHNs;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IGHNService
	{
		Task<CalculateFeeResponse?> CalculateShippingFeeAsync(CalculateFeeRequest request);
		Task<BaseResponse<List<ProvinceResponse>>> GetProvincesAsync();
		Task<BaseResponse<List<DistrictResponse>>> GetDistrictsByProvinceIdAsync(int provinceId);
		Task<BaseResponse<List<WardResponse>>> GetWardsByDistrictIdAsync(int districtId);
		Task<CreateShippingOrderResponse?> CreateShippingOrderAsync(CreateShippingOrderRequest request);
		Task<GetLeadTimeResponse?> GetLeadTimeAsync(GetLeadTimeRequest request);
		Task<bool> UpdateOrderCodAsync(UpdateCodRequest request);
		Task<List<ShippingOrderDetailDto>?> GetOrderDetailAsync(string orderCode);
		Task<bool> UpdateOrderAsync(UpdateOrderRequest request);
	}
}
