using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
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
	}
}
