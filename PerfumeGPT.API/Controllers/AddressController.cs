using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Requests.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Address.GHNs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHTKs;
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
		private readonly IGHTKService _ghtkService;
		private readonly IAddressService _addressService;

		public AddressController(IGHNService ghnService, IAddressService addressService, IGHTKService ghtkService)
		{
			_ghnService = ghnService;
			_addressService = addressService;
			_ghtkService = ghtkService;
		}

		[HttpGet]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<List<AddressResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<AddressResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<AddressResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<AddressResponse>>>> GetUserAddressesAsync()
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.GetUserAddressesAsync(userId);
			return HandleResponse(result);
		}

		[HttpGet("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<AddressResponse>>> GetAddressByIdAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.GetAddressByIdAsync(userId, id);
			return HandleResponse(result);
		}


		[HttpGet("default")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<AddressResponse>>> GetDefaultAddressAsync()
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.GetDefaultAddressAsync(userId);
			return HandleResponse(result);
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
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
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
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
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAddressAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.DeleteAddressAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}/set-default")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status403Forbidden)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SetDefaultAddressAsync([FromRoute] Guid id)
		{
			var userId = GetCurrentUserId();
			var result = await _addressService.SetDefaultAddressAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpGet("provinces")]
		[ProducesResponseType(typeof(BaseResponse<List<ProvinceResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<ProvinceResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<ProvinceResponse>>>> GetProvincesAsync()
		{
			var result = await _ghnService.GetProvincesAsync();
			var response = BaseResponse<List<ProvinceResponse>>.Ok(result, "Provinces retrieved successfully");
			return HandleResponse(response);
		}

		[HttpGet("districts")]
		[ProducesResponseType(typeof(BaseResponse<List<DistrictResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<DistrictResponse>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<List<DistrictResponse>>), StatusCodes.Status500InternalServerError)]
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

			var result = await _ghnService.GetDistrictsByProvinceIdAsync(provinceId);
			var response = BaseResponse<List<DistrictResponse>>.Ok(result, "Districts retrieved successfully");
			return HandleResponse(response);
		}

		[HttpGet("wards")]
		[ProducesResponseType(typeof(BaseResponse<List<WardResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<WardResponse>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<List<WardResponse>>), StatusCodes.Status500InternalServerError)]
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
			var result = await _ghnService.GetWardsByDistrictIdAsync(districtId);
			var response = BaseResponse<List<WardResponse>>.Ok(result, "Wards retrieved successfully");
			return HandleResponse(response);
		}

		[HttpGet("streets")]
		[ProducesResponseType(typeof(BaseResponse<AddressLevel4Response>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<AddressLevel4Response>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<AddressLevel4Response>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<AddressLevel4Response>>> GetStreetsAsync(
			[FromQuery] AddressLevel4Request request)
		{
			var result = await _ghtkService.GetAddressLevel4Async(request);
			return HandleResponse(BaseResponse<AddressLevel4Response>.Ok(result, "Streets retrieved successfully"));
		}
	}
}
