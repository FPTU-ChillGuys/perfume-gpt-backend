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

		public ProductsController(IProductService productService)
		{
			_productService = productService;
		}

		[HttpGet]
		public async Task<ActionResult<BaseResponse<PagedResult<ProductListItem>>>> GetProducts([FromQuery] GetPagedProductRequest request)
		{
			var response = await _productService.GetProductsAsync(request);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}")]
		public async Task<ActionResult<BaseResponse<ProductResponse>>> GetProduct(Guid productId)
		{
			var response = await _productService.GetProductAsync(productId);
			return HandleResponse(response);
		}

		[HttpPost]
		public async Task<ActionResult<BaseResponse<string>>> CreateProduct([FromBody] CreateProductRequest request)
		{
			var response = await _productService.CreateProductAsync(request);
			return HandleResponse(response);
		}

		[HttpPut("{productId:guid}")]
		public async Task<ActionResult<BaseResponse<string>>> UpdateProduct(Guid productId, [FromBody] UpdateProductRequest request)
		{
			var response = await _productService.UpdateProductAsync(productId, request);
			return HandleResponse(response);
		}

		[HttpDelete("{productId:guid}")]
		public async Task<ActionResult<BaseResponse<string>>> DeleteProduct(Guid productId)
		{
			var response = await _productService.DeleteProductAsync(productId);
			return HandleResponse(response);
		}

		// Media endpoints
		[HttpPost("{productId:guid}/images")]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> UploadProductImage(
			Guid productId,
			[FromForm] BulkUploadMediaRequest request)
		{
			var response = await _productService.UploadProductImageAsync(productId, request);
			return HandleResponse(response);
		}

		[HttpGet("{productId:guid}/images")]
		public async Task<ActionResult<BaseResponse<List<MediaResponse>>>> GetProductImages(Guid productId)
		{
			var response = await _productService.GetProductImagesAsync(productId);
			return HandleResponse(response);
		}

		[HttpDelete("{productId:guid}/images/{mediaId:guid}")]
		public async Task<ActionResult<BaseResponse<string>>> DeleteProductImage(Guid productId, Guid mediaId)
		{
			var response = await _productService.DeleteProductImageAsync(productId, mediaId);
			return HandleResponse(response);
		}

		[HttpPatch("{productId:guid}/images/{mediaId:guid}/set-primary")]
		public async Task<ActionResult<BaseResponse<string>>> SetPrimaryProductImage(Guid productId, Guid mediaId)
		{
			var response = await _productService.SetPrimaryProductImageAsync(productId, mediaId);
			return HandleResponse(response);
		}

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
    }
}


