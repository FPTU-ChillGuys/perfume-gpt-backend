using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductVariantsController : BaseApiController
	{
		private readonly IVariantService _variantService;

		public ProductVariantsController(IVariantService variantService)
		{
			_variantService = variantService;
		}

		[HttpGet]
		public async Task<ActionResult<BaseResponse<PagedResult<VariantPagedItem>>>> GetPagedVariants([FromQuery] GetPagedVariantsRequest request)
		{
			var result = await _variantService.GetPagedVariantsAsync(request);
			return HandleResponse(result);
		}

		[HttpGet("lookup")]
		public async Task<ActionResult<BaseResponse<List<VariantLookupItem>>>> GetVariantLookupList([FromQuery] Guid? productId = null)
		{
			var result = await _variantService.GetVariantLookupListAsync(productId);
			return HandleResponse(result);
		}

		[HttpGet("{variantId:guid}")]
		public async Task<ActionResult<BaseResponse<ProductVariantResponse>>> GetVariantById(Guid variantId)
		{
			var result = await _variantService.GetVariantByIdAsync(variantId);
			return HandleResponse(result);
		}

		[HttpPost]
		public async Task<ActionResult<BaseResponse<string>>> CreateVariant([FromForm] CreateVariantRequest request, IFormFile? imageFile)
		{
			FileUpload? fileUpload = null;
			if (imageFile != null && imageFile.Length > 0)
			{
				fileUpload = new FileUpload
				{
					FileStream = imageFile.OpenReadStream(),
					FileName = imageFile.FileName,
					Length = imageFile.Length,
					ContentType = imageFile.ContentType
				};
			}

			var result = await _variantService.CreateVariantAsync(request, fileUpload);
			return HandleResponse(result);
		}

		[HttpPut("{variantId:guid}")]
		public async Task<ActionResult<BaseResponse<string>>> UpdateVariant(Guid variantId, [FromForm] UpdateVariantRequest request, IFormFile? imageFile)
		{
			FileUpload? fileUpload = null;
			if (imageFile != null && imageFile.Length > 0)
			{
				fileUpload = new FileUpload
				{
					FileStream = imageFile.OpenReadStream(),
					FileName = imageFile.FileName,
					Length = imageFile.Length,
					ContentType = imageFile.ContentType
				};
			}

			var result = await _variantService.UpdateVariantAsync(variantId, request, fileUpload);
			return HandleResponse(result);
		}

		[HttpDelete("{variantId:guid}")]
		public async Task<ActionResult<BaseResponse<string>>> DeleteVariant(Guid variantId)
		{
			var result = await _variantService.DeleteVariantAsync(variantId);
			return HandleResponse(result);
		}

	}
}
