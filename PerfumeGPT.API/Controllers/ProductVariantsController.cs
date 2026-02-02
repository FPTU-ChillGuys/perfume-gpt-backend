using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductVariantsController : BaseApiController
	{
		private readonly IVariantService _variantService;
		private readonly IMediaService _mediaService;

		public ProductVariantsController(IVariantService variantService, IMediaService mediaService)
		{
			_variantService = variantService;
			_mediaService = mediaService;
		}

		#region CRUD Endpoints

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VariantPagedItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<VariantPagedItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<VariantPagedItem>>>> GetPagedVariants([FromQuery] GetPagedVariantsRequest request)
		{
			var result = await _variantService.GetPagedVariantsAsync(request);
			return HandleResponse(result);
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<VariantLookupItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<VariantLookupItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<VariantLookupItem>>>> GetVariantLookupList([FromQuery] Guid? productId = null)
		{
			var result = await _variantService.GetVariantLookupListAsync(productId);
			return HandleResponse(result);
		}

		[HttpGet("{variantId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ProductVariantResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ProductVariantResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ProductVariantResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ProductVariantResponse>>> GetVariantById(Guid variantId)
		{
			var result = await _variantService.GetVariantByIdAsync(variantId);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<string>>>> CreateVariant([FromBody] CreateVariantRequest request)
		{
			var validation = ValidateRequestBody<CreateVariantRequest>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _variantService.CreateVariantAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{variantId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<string>>>> UpdateVariant(Guid variantId, [FromBody] UpdateVariantRequest request)
		{
			var validation = ValidateRequestBody<UpdateVariantRequest>(request);
			if (validation != null)
			{
				return validation;
			}

			var result = await _variantService.UpdateVariantAsync(variantId, request);
			return HandleResponse(result);
		}

		[HttpDelete("{variantId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteVariant(Guid variantId)
		{
			var result = await _variantService.DeleteVariantAsync(variantId);
			return HandleResponse(result);
		}

		#endregion

		#region Media Endpoints

		[HttpGet("{variantId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetVariantImages(Guid variantId)
		{
			var response = await _variantService.GetVariantImagesAsync(variantId);
			return HandleResponse(response);
		}

		[HttpGet("{variantId:guid}/images/primary")]
		[ProducesResponseType(typeof(BaseResponse<MediaResponse?>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<MediaResponse?>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<MediaResponse?>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<MediaResponse?>>> GetVariantPrimaryImage(Guid variantId)
		{
			var response = await _mediaService.GetPrimaryMediaAsync(EntityType.ProductVariant, variantId);
			return HandleResponse(response);
		}

		[HttpPut("images/{mediaId:guid}/set-primary")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SetVariantPrimaryImage(Guid mediaId)
		{
			var response = await _mediaService.SetPrimaryMediaAsync(mediaId);
			return HandleResponse(response);
		}

		[HttpPost("images/temporary")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages(
			[FromForm] VariantUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadTemporaryVariantMediaAsync(userId, request);
			return HandleResponse(response);
		}

		#endregion
	}
}
