using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductsController : BaseApiController
	{
		private readonly IProductService _productService;
		private readonly IMediaService _mediaService;

		public ProductsController(
			IProductService productService,
			IMediaService mediaService)
		{
			_productService = productService;
			_mediaService = mediaService;
		}

		#region CRUD Endpoints
		[HttpGet("{productId:guid}/information")]
		[ProducesResponseType(typeof(BaseResponse<ProductInforResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ProductInforResponse>>> GetProductDetail([FromRoute] Guid productId)
		{
			var result = await _productService.GetProductInforAsync(productId);
			return HandleResponse(result);
		}

		[HttpGet("{productId:guid}/fast-look")]
		[ProducesResponseType(typeof(BaseResponse<ProductFastLookResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ProductFastLookResponse>>> GetProductFastLook([FromRoute] Guid productId)
		{
			var result = await _productService.GetProductFastLookAsync(productId);
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<ProductLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ProductLookupItem>>>> GetProductLookupList()
		{
			var result = await _productService.GetProductLookupListAsync();
			return HandleResponse(result);
		}

		[HttpGet("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<PublicProductResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PublicProductResponse>>> GetProduct([FromRoute] Guid productId)
		{
			var response = await _productService.GetPublicProductAsync(productId);
			return HandleResponse(response);
		}

		[HttpGet("/api/admin/products/{productId:guid}")]
		[Authorize(Roles = "admin,staff")]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<ProductResponse>>> GetAdminProduct([FromRoute] Guid productId)
		{
			var response = await _productService.GetAdminProductAsync(productId);
			return HandleResponse(response);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<string>>>> CreateProduct([FromBody] CreateProductRequest request)
		{
			var response = await _productService.CreateProductAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<string>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<string>>>> UpdateProduct([FromRoute] Guid productId, [FromBody] UpdateProductRequest request)
		{
			var response = await _productService.UpdateProductAsync(productId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> DeleteProduct([FromRoute] Guid productId)
		{
			var response = await _productService.DeleteProductAsync(productId);
			return HandleResponse(response);
		}
		#endregion CRUD Endpoints



		#region Media Endpoints
		[HttpPost("images/temporary")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BulkActionResult<List<TemporaryMediaResponse>>>>> UploadTemporaryImages([FromForm] ProductUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();

			var response = await _mediaService.UploadProductTemporaryMediaAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetProductImages([FromRoute] Guid productId)
		{
			var response = await _productService.GetProductImagesAsync(productId);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}/images/primary")]
		[ProducesResponseType(typeof(BaseResponse<MediaResponse?>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<MediaResponse?>>> GetProductPrimaryImage([FromRoute] Guid productId)
		{
			var response = await _mediaService.GetPrimaryMediaAsync(EntityType.Product, productId);
			return HandleResponse(response);
		}

		[HttpPut("images/{mediaId:guid}/set-primary")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<string>>> SetProductPrimaryImage([FromRoute] Guid mediaId)
		{
			var response = await _mediaService.SetPrimaryMediaAsync(mediaId);
			return HandleResponse(response);
		}
		#endregion Media Endpoints



		#region Product Recommendations
		[HttpGet("daily-sale-figures")]
		[ProducesResponseType(typeof(BaseResponse<List<ProductDailySaleFigureResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<ProductDailySaleFigureResponse>>>> GetProductDailySaleFigures([FromQuery] DateOnly date)
		{
			var response = await _productService.GetProductDailySaleFiguresAsync(date);
			return HandleResponse(response);
		}
		#endregion Product Recommendations



		#region Best Seller & New Arrival Endpoints
		[HttpGet("best-sellers")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetBestSellerProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetBestSellerProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("new-arrivals")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetNewArrivalProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetNewArrivalProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("campaigns/{campaignId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetCampaignProducts([FromRoute] Guid campaignId, [FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetCampaignProductsAsync(campaignId, request);
			return HandleResponse(response);
		}
		#endregion Best Seller & New Arrival Endpoints
	}
}


