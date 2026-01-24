using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Application.Services
{
	public class ShippingService : IShippingService
	{
		private readonly IGHNService _ghnService;

		public ShippingService(IGHNService ghnService)
		{
			_ghnService = ghnService;
		}

		public async Task<decimal?> CalculateShippingFeeAsync(int districtId, string wardCode)
		{
			try
			{
				var calculateShippingFeeRequest = new CalculateFeeRequest
				{
					ToDistrictId = districtId,
					ToWardCode = wardCode
				};

				var shippingFeeResponse = await _ghnService.CalculateShippingFeeAsync(calculateShippingFeeRequest);
				return shippingFeeResponse?.Data?.Total;
			}
			catch
			{
				return null;
			}
		}
	}
}
