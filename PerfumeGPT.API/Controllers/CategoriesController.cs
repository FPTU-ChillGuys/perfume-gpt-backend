using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoriesController : BaseApiController
	{
		private readonly ICategoryService categoryService;

		public CategoriesController(ICategoryService categoryService)
		{
			this.categoryService = categoryService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<CategoriesLookupItem>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<CategoriesLookupItem>>>> GetCategoryLookupAsync()
		{
			var result = await categoryService.GetCategoryLookupAsync();
			return HandleResponse(result);
		}
	}
}
