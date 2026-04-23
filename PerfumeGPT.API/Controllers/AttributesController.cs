using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AttributesController : BaseApiController
	{
		private readonly IAttributeService _attributeService;
		private readonly IAttributeValueService _attributeValueService;
		private readonly IValidator<CreateAttributeRequest> _createAttributeValidator;
		private readonly IValidator<UpdateAttributeRequest> _updateAttributeValidator;
		private readonly IValidator<CreateAttributeValueRequest> _createAttributeValueValidator;
		private readonly IValidator<UpdateAttributeValueRequest> _updateAttributeValueValidator;

		public AttributesController(
			IAttributeService attributeService,
			IAttributeValueService attributeValueService,
			IValidator<CreateAttributeRequest> createAttributeValidator,
			IValidator<UpdateAttributeRequest> updateAttributeValidator,
			IValidator<CreateAttributeValueRequest> createAttributeValueValidator,
			IValidator<UpdateAttributeValueRequest> updateAttributeValueValidator)
		{
			_attributeService = attributeService;
			_attributeValueService = attributeValueService;
			_createAttributeValidator = createAttributeValidator;
			_updateAttributeValidator = updateAttributeValidator;
			_createAttributeValueValidator = createAttributeValueValidator;
			_updateAttributeValueValidator = updateAttributeValueValidator;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<AttributeLookupItem>>>> GetAttributeLookupList([FromQuery] bool isVariantLevel)
		{
			var result = await _attributeService.GetLookupListAsync(isVariantLevel);
			return HandleResponse(result);
		}

		[HttpGet("{attributeId:int}/values/lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeValueLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<AttributeValueLookupItem>>>> GetAttributeValueLookupList([FromRoute] int attributeId)
		{
			var validationError = ValidatePositiveInt(attributeId, "Attribute ID");
			if (validationError != null) return validationError;

			var result = await _attributeValueService.GetLookupListByAttributeIdAsync(attributeId);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateAttribute([FromBody] CreateAttributeRequest request)
		{
			var validation = await ValidateRequestAsync(_createAttributeValidator, request);
			if (validation != null) return validation;

			var result = await _attributeService.CreateAttributeAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{attributeId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAttribute([FromRoute] int attributeId, [FromBody] UpdateAttributeRequest request)
		{
			var validationError = ValidatePositiveInt(attributeId, "Attribute ID");
			if (validationError != null) return validationError;

			var validation = await ValidateRequestAsync(_updateAttributeValidator, request);
			if (validation != null) return validation;

			var result = await _attributeService.UpdateAttributeAsync(attributeId, request);
			return HandleResponse(result);
		}

		[HttpDelete("{attributeId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAttribute([FromRoute] int attributeId)
		{
			var validationError = ValidatePositiveInt(attributeId, "Attribute ID");
			if (validationError != null) return validationError;

			var result = await _attributeService.DeleteAttributeAsync(attributeId);
			return HandleResponse(result);
		}

		[HttpPost("{attributeId:int}/values")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateAttributeValue([FromRoute] int attributeId, [FromBody] CreateAttributeValueRequest request)
		{
			var validationError = ValidatePositiveInt(attributeId, "Attribute ID");
			if (validationError != null) return validationError;

			var validation = await ValidateRequestAsync(_createAttributeValueValidator, request);
			if (validation != null) return validation;

			var result = await _attributeValueService.CreateAttributeValueAsync(attributeId, request);
			return HandleResponse(result);
		}

		[HttpPut("values/{valueId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAttributeValue([FromRoute] int valueId, [FromBody] UpdateAttributeValueRequest request)
		{
			var validationError = ValidatePositiveInt(valueId, "Attribute Value ID");
			if (validationError != null) return validationError;

			var validation = await ValidateRequestAsync(_updateAttributeValueValidator, request);
			if (validation != null) return validation;

			var result = await _attributeValueService.UpdateAttributeValueAsync(valueId, request);
			return HandleResponse(result);
		}

		[HttpDelete("values/{valueId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAttributeValue([FromRoute] int valueId)
		{
			var validationError = ValidatePositiveInt(valueId, "Attribute Value ID");
			if (validationError != null) return validationError;

			var result = await _attributeValueService.DeleteAttributeValueAsync(valueId);
			return HandleResponse(result);
		}
	}
}
