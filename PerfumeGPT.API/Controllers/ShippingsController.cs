using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.GHNs;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ShippingsController : BaseApiController
	{
		private readonly IGHNService _ghnService;
		public ShippingsController(IGHNService shippingService)
		{
			_ghnService = shippingService;
		}

		[HttpPost("calculate-fee")]
		public async Task<ActionResult<BaseResponse<CalculateFeeResponse>>> CalculateShippingFeeAsync([FromBody] CalculateFeeRequest request)
		{
			var validation = ValidateRequestBody<CalculateFeeRequest>(request);
			if (validation != null) return validation;
			
			try
			{
				var result = await _ghnService.CalculateShippingFeeAsync(request);
				var response = BaseResponse<CalculateFeeResponse>.Ok(result, "Shipping fee calculated successfully");
				return HandleResponse(response);
			}
			catch (Exception ex)
			{
				var errorResponse = BaseResponse<CalculateFeeResponse>.Fail(
					$"Failed to calculate shipping fee: {ex.Message}",
					ResponseErrorType.InternalError
				);
				return HandleResponse(errorResponse);
			}
		}
	}
}
