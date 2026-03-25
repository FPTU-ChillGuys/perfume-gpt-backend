using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BrandsController : BaseApiController
	{
		private readonly IBrandService _brandService;

		public BrandsController(IBrandService brandService)
		{
			_brandService = brandService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<BrandLookupItem>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<BrandLookupItem>>>> GetBrandLookupAsync()
		{
			var result = await _brandService.GetBrandLookupAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<BrandResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<BrandResponse>>>> GetAllBrandsAsync()
		{
			var result = await _brandService.GetAllBrandsAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> GetBrandByIdAsync(int id)
		{
			var result = await _brandService.GetBrandByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> CreateBrandAsync([FromBody] CreateBrandRequest request)
		{
			var result = await _brandService.CreateBrandAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> UpdateBrandAsync(int id, [FromBody] UpdateBrandRequest request)
		{
			var result = await _brandService.UpdateBrandAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteBrandAsync(int id)
		{
			var result = await _brandService.DeleteBrandAsync(id);
			return HandleResponse(result);
		}


	}
}
