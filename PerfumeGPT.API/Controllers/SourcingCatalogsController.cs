using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SourcingCatalogsController : BaseApiController
	{
		private readonly ISourcingCatalogService _sourcingCatalogService;
		private readonly IValidator<CreateCatalogItemRequest> _createValidator;
		private readonly IValidator<UpdateCatalogItemRequest> _updateValidator;

		public SourcingCatalogsController(
			ISourcingCatalogService sourcingCatalogService,
			IValidator<CreateCatalogItemRequest> createValidator,
			IValidator<UpdateCatalogItemRequest> updateValidator)
		{
			_sourcingCatalogService = sourcingCatalogService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<IEnumerable<CatalogItemResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<IEnumerable<CatalogItemResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<IEnumerable<CatalogItemResponse>>>> GetCatalogs(
			[FromQuery] int? supplierId,
			[FromQuery] Guid? variantId)
		{
			var result = await _sourcingCatalogService.GetCatalogsAsync(supplierId, variantId);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateCatalogItem([FromBody] CreateCatalogItemRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var result = await _sourcingCatalogService.CreateItemAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCatalogItem([FromRoute] Guid id, [FromBody] UpdateCatalogItemRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var result = await _sourcingCatalogService.UpdateItemAsync(id, request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}/set-primary")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SetAsPrimary([FromRoute] Guid id)
		{
			var result = await _sourcingCatalogService.SetAsPrimaryAsync(id);
			return HandleResponse(result);
		}

		[HttpDelete("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCatalogItem([FromRoute] Guid id)
		{
			var result = await _sourcingCatalogService.DeleteItemAsync(id);
			return HandleResponse(result);
		}
	}
}
