using FluentValidation;
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
		private readonly IValidator<CreateAddressRequest> _createValidator;
		private readonly IValidator<UpdateAddressRequest> _updateValidator;

		public AddressController(IGHNService ghnService,
			IAddressService addressService,
			IGHTKService ghtkService,
			IValidator<CreateAddressRequest> createValidator,
			IValidator<UpdateAddressRequest> updateValidator)
		{
			_ghnService = ghnService;
			_addressService = addressService;
			_ghtkService = ghtkService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<List<AddressResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<AddressResponse>>>> GetUserAddressesAsync()
		{
			var userId = GetCurrentUserId();

			var result = await _addressService.GetUserAddressesAsync(userId);
			return HandleResponse(result);
		}

		[HttpGet("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<AddressResponse>>> GetAddressByIdAsync([FromRoute] Guid id)
		{
			var validationError = ValidateNotEmptyGuid(id, "Address ID");
			if (validationError != null) return validationError;

			var userId = GetCurrentUserId();

			var result = await _addressService.GetAddressByIdAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpGet("default")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<AddressResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<AddressResponse>>> GetDefaultAddressAsync()
		{
			var userId = GetCurrentUserId();

			var result = await _addressService.GetDefaultAddressAsync(userId);
			return HandleResponse(result);
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateAddressAsync([FromBody] CreateAddressRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();

			var result = await _addressService.CreateAddressAsync(userId, request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAddressAsync([FromRoute] Guid id, [FromBody] UpdateAddressRequest request)
		{
			var validationError = ValidateNotEmptyGuid(id, "Address ID");
			if (validationError != null) return validationError;

			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var userId = GetCurrentUserId();

			var result = await _addressService.UpdateAddressAsync(userId, id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id:guid}")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAddressAsync([FromRoute] Guid id)
		{
			var validationError = ValidateNotEmptyGuid(id, "Address ID");
			if (validationError != null) return validationError;

			var userId = GetCurrentUserId();

			var result = await _addressService.DeleteAddressAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}/set-default")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> SetDefaultAddressAsync([FromRoute] Guid id)
		{
			var validationError = ValidateNotEmptyGuid(id, "Address ID");
			if (validationError != null) return validationError;

			var userId = GetCurrentUserId();

			var result = await _addressService.SetDefaultAddressAsync(userId, id);
			return HandleResponse(result);
		}

		[HttpGet("provinces")]
		[ProducesResponseType(typeof(BaseResponse<List<ProvinceResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ProvinceResponse>>>> GetProvincesAsync()
		{
			var response = await _ghnService.GetProvincesAsync();
			return HandleResponse(response);
		}

		[HttpGet("districts")]
		[ProducesResponseType(typeof(BaseResponse<List<DistrictResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<DistrictResponse>>>> GetDistrictsByProvinceIdAsync([FromQuery] int provinceId)
		{
			var validationError = ValidatePositiveInt(provinceId, "Province ID");
			if (validationError != null) return validationError;

			var response = await _ghnService.GetDistrictsByProvinceIdAsync(provinceId);
			return HandleResponse(response);
		}

		[HttpGet("wards")]
		[ProducesResponseType(typeof(BaseResponse<List<WardResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<WardResponse>>>> GetWardsByDistrictIdAsync([FromQuery] int districtId)
		{
			var validationError = ValidatePositiveInt(districtId, "District ID");
			if (validationError != null) return validationError;

			var response = await _ghnService.GetWardsByDistrictIdAsync(districtId);
			return HandleResponse(response);
		}

		[HttpGet("streets")]
		[ProducesResponseType(typeof(BaseResponse<AddressLevel4Response>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<AddressLevel4Response>>> GetStreetsAsync([FromQuery] GetAddressLevel4Request request)
		{
			var response = await _ghtkService.GetAddressLevel4Async(request);
			return HandleResponse(response);
		}
	}
}
