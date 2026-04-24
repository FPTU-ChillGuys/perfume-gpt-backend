using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Categories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CategoriesController : BaseApiController
	{
		private readonly ICategoryService _categoryService;

		public CategoriesController(ICategoryService categoryService)
		{
			_categoryService = categoryService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<CategoriesLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<CategoriesLookupItem>>>> GetCategoryLookupAsync()
		{
			var result = await _categoryService.GetCategoryLookupAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<CategoryResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<CategoryResponse>>>> GetAllCategoriesAsync()
		{
			var result = await _categoryService.GetAllCategoriesAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<CategoryResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CategoryResponse>>> GetCategoryByIdAsync([FromRoute] int id)
		{
			var validationResult = ValidatePositiveInt(id, "Category ID");
			if (validationResult != null) return validationResult;

			var result = await _categoryService.GetCategoryByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<CategoryResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CategoryResponse>>> CreateCategoryAsync([FromBody] CreateCategoryRequest request)
		{
			var result = await _categoryService.CreateCategoryAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<CategoryResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<CategoryResponse>>> UpdateCategoryAsync([FromRoute] int id, [FromBody] UpdateCategoryRequest request)
		{
			var validationResult = ValidatePositiveInt(id, "Category ID");
			if (validationResult != null) return validationResult;

			var result = await _categoryService.UpdateCategoryAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteCategoryAsync([FromRoute] int id)
		{
			var validationResult = ValidatePositiveInt(id, "Category ID");
			if (validationResult != null) return validationResult;

			var result = await _categoryService.DeleteCategoryAsync(id);
			return HandleResponse(result);
		}
	}
}
