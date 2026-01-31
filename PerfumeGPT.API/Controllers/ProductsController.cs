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

		public ProductsController(IProductService productService)
		{
			_productService = productService;
		}

		/// <summary>
		/// Get paginated list of products
		/// </summary>
		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<PagedResult<ProductListItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetProductsAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get product by ID
		/// </summary>
		[HttpGet("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<ProductResponse>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<ProductResponse>>> GetProduct(Guid productId)
		{
			var response = await _productService.GetProductAsync(productId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Create a new product
		/// </summary>
		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> CreateProduct([FromBody] CreateProductRequest request)
		{
			var response = await _productService.CreateProductAsync(request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Update an existing product
		/// </summary>
		[HttpPut("{productId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
		{
			var response = await _productService.UpdateProductAsync(productId, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Delete a product
		/// </summary>
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

		/// <summary>
		/// Upload product images
		/// </summary>
		[HttpPost("{productId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> UploadProductImage(
			Guid productId,
			[FromForm] BulkUploadMediaRequest request)
		{
			var response = await _productService.UploadProductImageAsync(productId, request);
			return HandleResponse(response);
		}

		/// <summary>
		/// Get all images for a product
		/// </summary>
		[HttpGet("{productId:guid}/images")]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<List<MediaResponse>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetProductImages(Guid productId)
		{
			var response = await _productService.GetProductImagesAsync(productId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Delete a product image
		/// </summary>
		[HttpDelete("{productId:guid}/images/{mediaId:guid}")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> DeleteProductImage(Guid productId, Guid mediaId)
		{
			var response = await _productService.DeleteProductImageAsync(productId, mediaId);
			return HandleResponse(response);
		}

		/// <summary>
		/// Set a product image as primary
		/// </summary>
		[HttpPatch("{productId:guid}/images/{mediaId:guid}/set-primary")]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(BaseResponse<string>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<string>>> SetPrimaryProductImage(Guid productId, Guid mediaId)
		{
			var response = await _productService.SetPrimaryProductImageAsync(productId, mediaId);
			return HandleResponse(response);
		}
	}
}


