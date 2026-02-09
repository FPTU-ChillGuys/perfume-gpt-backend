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

		public AttributesController(IAttributeService attributeService, IAttributeValueService attributeValueService)
		{
			_attributeService = attributeService;
			_attributeValueService = attributeValueService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeLookupItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeLookupItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<AttributeLookupItem>>>> GetAttributeLookupList([FromQuery] bool? isVariantLevel)
		{
			var result = await _attributeService.GetLookupListAsync(isVariantLevel);
			return HandleResponse(result);
		}

		[HttpGet("values/lookup/{attributeId:int}")]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeValueLookupItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<AttributeValueLookupItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<AttributeValueLookupItem>>>> GetAttributeValueLookupList([FromRoute] int attributeId)
		{
			var result = await _attributeValueService.GetLookupListByAttributeIdAsync(attributeId);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> CreateAttribute([FromBody] CreateAttributeRequest request)
		{
			var result = await _attributeService.CreateAttributeAsync(request);
			if (!result.Success) return HandleResponse(result);
			return CreatedAtAction(nameof(GetAttributeLookupList), new { }, result);
		}

		[HttpPut("{attributeId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAttribute([FromRoute] int attributeId, [FromBody] UpdateAttributeRequest request)
		{
			var result = await _attributeService.UpdateAttributeAsync(attributeId, request);
			return HandleResponse(result);
		}

		[HttpDelete("{attributeId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAttribute([FromRoute] int attributeId)
		{
			var result = await _attributeService.DeleteAttributeAsync(attributeId);
			return HandleResponse(result);
		}

		[HttpPost("values")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status201Created)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> CreateAttributeValue([FromBody] CreateAttributeValueRequest request)
		{
			var result = await _attributeValueService.CreateAttributeValueAsync(request);
			if (!result.Success) return HandleResponse(result);
			return CreatedAtAction(nameof(GetAttributeValueLookupList), new { attributeId = request.AttributeId }, result);
		}

		[HttpPut("values/{valueId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateAttributeValue([FromRoute] int valueId, [FromBody] UpdateAttributeValueRequest request)
		{
			var result = await _attributeValueService.UpdateAttributeValueAsync(valueId, request);
			return HandleResponse(result);
		}

		[HttpDelete("values/{valueId:int}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteAttributeValue([FromRoute] int valueId)
		{
			var result = await _attributeValueService.DeleteAttributeValueAsync(valueId);
			return HandleResponse(result);
		}
	}
}
