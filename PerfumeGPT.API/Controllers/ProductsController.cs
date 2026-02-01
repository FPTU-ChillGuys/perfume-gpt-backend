using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Media;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Products;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ProductsController : BaseApiController
	{
		private readonly IProductService _productService;
		private readonly IMediaService _mediaService;

		public ProductsController(IProductService productService, IMediaService mediaService)
		{
			_productService = productService;
			_mediaService = mediaService;
		}

		#region CRUD Endpoints
		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ProductResponse>>> GetProduct(Guid productId)
		{
			var response = await _productService.GetProductAsync(productId);
			return HandleResponse(response);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateProduct([FromBody] CreateProductRequest request)
		{
			var validation = ValidateRequestBody<CreateProductRequest>(request);
			if (validation != null)
			{
				return validation;
			}

			var response = await _productService.CreateProductAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
		{
			var validation = ValidateRequestBody<UpdateProductRequest>(request);
			if (validation != null)
			{
				return validation;
			}

			var response = await _productService.UpdateProductAsync(productId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteProduct(Guid productId)
		{
			var response = await _productService.DeleteProductAsync(productId);
			return HandleResponse(response);
		}
		#endregion

		#region Media Endpoints

		[HttpPost("images/temporary")]
		[Authorize]
		[ProducesResponseType(typeof(BaseResponse<List<TemporaryMediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<TemporaryMediaResponse>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<List<TemporaryMediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<TemporaryMediaResponse>>>> UploadTemporaryImages(
			[FromForm] ProductUploadMediaRequest request)
		{
			var userId = GetCurrentUserId();
			var response = await _mediaService.UploadTemporaryProductMediaAsync(userId, request);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetProductImages(Guid productId)
		{
			var response = await _productService.GetProductImagesAsync(productId);
			return HandleResponse(response);
		}

		#endregion

		#region Product Recommendations
		[HttpPost("embeddings/update/alls")]
		public async Task<ActionResult<BaseResponse>> UpdateAllProductEmbeddings()
		{
			var response = await _productService.UpdateAllProductsEmbeddingAsync();
			return HandleResponse(response);
		}

		[HttpPost("embeddings/update/{productId:guid}")]
		public async Task<ActionResult<BaseResponse>> UpdateProductEmbeddings(Guid productId)
		{
			var response = await _productService.UpdateProductEmbeddingAsync(productId);
			return HandleResponse(response);
		}

		[HttpGet("search/semantic")]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetSemanticSearchProducts([FromQuery] string searchText, [FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetSemanticSearchProductAsync(searchText, request);
			return HandleResponse(response);
		}
		#endregion
	}
}


