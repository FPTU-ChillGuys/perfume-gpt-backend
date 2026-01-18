using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Base;
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
	}
}
