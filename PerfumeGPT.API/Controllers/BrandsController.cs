using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BrandsController : BaseApiController
	{
		private readonly IBrandService _brandService;
		private readonly IValidator<CreateBrandRequest> _createValidator;
		private readonly IValidator<UpdateBrandRequest> _updateValidator;

		public BrandsController(
			IBrandService brandService,
			IValidator<UpdateBrandRequest> updateValidator,
			IValidator<CreateBrandRequest> createValidator)
		{
			_brandService = brandService;
			_updateValidator = updateValidator;
			_createValidator = createValidator;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<BrandLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<BrandLookupItem>>>> GetBrandLookupAsync()
		{
			var result = await _brandService.GetBrandLookupAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<BrandResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<BrandResponse>>>> GetAllBrandsAsync()
		{
			var result = await _brandService.GetAllBrandsAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> GetBrandByIdAsync([FromRoute] int id)
		{
			var result = await _brandService.GetBrandByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> CreateBrandAsync([FromBody] CreateBrandRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var result = await _brandService.CreateBrandAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<BrandResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<BrandResponse>>> UpdateBrandAsync([FromRoute] int id, [FromBody] UpdateBrandRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var result = await _brandService.UpdateBrandAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteBrandAsync([FromRoute] int id)
		{
			var result = await _brandService.DeleteBrandAsync(id);
			return HandleResponse(result);
		}
	}
}
