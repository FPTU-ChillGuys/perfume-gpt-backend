using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Requests.GHNs.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AddressController : BaseApiController
	{
		private readonly IGHNService _ghnService;
		private readonly IAddressService _addressService;

		public AddressController(IGHNService ghnService, IAddressService addressService)
		{
			_ghnService = ghnService;
			_addressService = addressService;
		}

		[HttpGet]
		[Authorize]
		public async Task<ActionResult<BaseResponse<List<AddressResponse>>>> GetUserAddressesAsync()
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.GetUserAddressesAsync(userId);
			return HandleResponse(result);
		}

		[HttpPost]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> CreateAddressAsync([FromBody] CreateAddressRequest request)
		{
			var validation = ValidateRequestBody<CreateAddressRequest>(request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();
			var result = await _addressService.CreateAddressAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAddressAsync([FromRoute] Guid id, [FromBody] UpdateAddressRequest request)
		{
			var validation = ValidateRequestBody<UpdateAddressRequest>(request);
			if (validation != null) return validation;
			var userId = GetCurrentUserId();
			var result = await _addressService.UpdateAddressAsync(userId, id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id:guid}")]
		[Authorize]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAddressAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.DeleteAddressAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpGet("provinces")]
		public async Task<ActionResult<BaseResponse<List<ProvinceResponse>>>> GetProvincesAsync()
		{
			try
			{
				var result = await _ghnService.GetProvincesAsync();
				var response = BaseResponse<List<ProvinceResponse>>.Ok(result, "Provinces retrieved successfully");
				return HandleResponse(response);
			}
			catch (Exception ex)
			{
				var errorResponse = BaseResponse<List<ProvinceResponse>>.Fail(
					$"Failed to get provinces: {ex.Message}",
					ResponseErrorType.InternalError
				);
				return HandleResponse(errorResponse);
			}
		}

		[HttpGet("districts")]
		public async Task<ActionResult<BaseResponse<List<DistrictResponse>>>> GetDistrictsByProvinceIdAsync([FromQuery] int provinceId)
		{
			if (provinceId <= 0)
			{
				var badRequestResponse = BaseResponse<List<DistrictResponse>>.Fail(
					"Province ID must be greater than 0",
					ResponseErrorType.BadRequest
				);
				return HandleResponse(badRequestResponse);
			}

			try
			{
				var result = await _ghnService.GetDistrictsByProvinceIdAsync(provinceId);
				var response = BaseResponse<List<DistrictResponse>>.Ok(result, "Districts retrieved successfully");
				return HandleResponse(response);
			}
			catch (Exception ex)
			{
				var errorResponse = BaseResponse<List<DistrictResponse>>.Fail(
					$"Failed to get districts: {ex.Message}",
					ResponseErrorType.InternalError
				);
				return HandleResponse(errorResponse);
			}
		}

		[HttpGet("wards")]
		public async Task<ActionResult<BaseResponse<List<WardResponse>>>> GetWardsByDistrictIdAsync([FromQuery] int districtId)
		{
			if (districtId <= 0)
			{
				var badRequestResponse = BaseResponse<List<WardResponse>>.Fail(
					"District ID must be greater than 0",
					ResponseErrorType.BadRequest
				);
				return HandleResponse(badRequestResponse);
			}

			try
			{
				var result = await _ghnService.GetWardsByDistrictIdAsync(districtId);
				var response = BaseResponse<List<WardResponse>>.Ok(result, "Wards retrieved successfully");
				return HandleResponse(response);
			}
			catch (Exception ex)
			{
				var errorResponse = BaseResponse<List<WardResponse>>.Fail(
					$"Failed to get wards: {ex.Message}",
					ResponseErrorType.InternalError
				);
				return HandleResponse(errorResponse);
			}
		}
	}
}
