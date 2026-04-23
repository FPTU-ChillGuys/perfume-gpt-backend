using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SuppliersController : BaseApiController
	{
		private readonly ISupplierService _supplierService;
		private readonly IValidator<CreateSupplierRequest> _createValidator;
		private readonly IValidator<UpdateSupplierRequest> _updateValidator;

		public SuppliersController(
			ISupplierService supplierService,
			IValidator<CreateSupplierRequest> createValidator,
			IValidator<UpdateSupplierRequest> updateValidator)
		{
			_supplierService = supplierService;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierLookupItem>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<SupplierLookupItem>>>> GetSupplierLookupList()
		{
			var result = await _supplierService.GetSupplierLookupListAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierResponse>>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<List<SupplierResponse>>>> GetAllSuppliersAsync()
		{
			var result = await _supplierService.GetAllSuppliersAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> GetSupplierByIdAsync([FromRoute] int id)
		{
			var result = await _supplierService.GetSupplierByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> CreateSupplierAsync([FromBody] CreateSupplierRequest request)
		{
			var validation = await ValidateRequestAsync(_createValidator, request);
			if (validation != null) return validation;

			var result = await _supplierService.CreateSupplierAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> UpdateSupplierAsync([FromRoute] int id, [FromBody] UpdateSupplierRequest request)
		{
			var validation = await ValidateRequestAsync(_updateValidator, request);
			if (validation != null) return validation;

			var result = await _supplierService.UpdateSupplierAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesDefaultResponseType(typeof(BaseResponse))]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteSupplierAsync([FromRoute] int id)
		{
			var result = await _supplierService.DeleteSupplierAsync(id);
			return HandleResponse(result);
		}
	}
}
