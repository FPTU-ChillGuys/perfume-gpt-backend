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

		public SourcingCatalogsController(ISourcingCatalogService sourcingCatalogService)
		{
			_sourcingCatalogService = sourcingCatalogService;
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<IEnumerable<CatalogItemResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<IEnumerable<CatalogItemResponse>>>> GetCatalogs([FromQuery] int? supplierId, [FromQuery] Guid? variantId)
		{
			var result = await _sourcingCatalogService.GetCatalogsAsync(supplierId, variantId);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> CreateCatalogItem([FromBody] CreateCatalogItemRequest request)
		{
			var result = await _sourcingCatalogService.CreateItemAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> UpdateCatalogItem([FromRoute] Guid id, [FromBody] UpdateCatalogItemRequest request)
		{
			var result = await _sourcingCatalogService.UpdateItemAsync(id, request);
			return HandleResponse(result);
		}

		[HttpPut("{id:guid}/set-primary")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> SetAsPrimary([FromRoute] Guid id)
		{
			var result = await _sourcingCatalogService.SetAsPrimaryAsync(id);
			return HandleResponse(result);
		}

		[HttpDelete("{id:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteCatalogItem([FromRoute] Guid id)
		{
			var result = await _sourcingCatalogService.DeleteItemAsync(id);
			return HandleResponse(result);
		}
	}
}
