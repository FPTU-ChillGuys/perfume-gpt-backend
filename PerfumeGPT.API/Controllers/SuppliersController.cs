using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Suppliers;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class SuppliersController : BaseApiController
	{
		private readonly ISupplierService _supplierService;

		public SuppliersController(ISupplierService supplierService)
		{
			_supplierService = supplierService;
		}

		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierLookupItem>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<SupplierLookupItem>>>> GetSupplierLookupList()
		{
			var result = await _supplierService.GetSupplierLookupListAsync();
			return HandleResponse(result);
		}

		[HttpGet]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierResponse>>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<List<SupplierResponse>>>> GetAllSuppliersAsync()
		{
			var result = await _supplierService.GetAllSuppliersAsync();
			return HandleResponse(result);
		}

		[HttpGet("{id}")]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> GetSupplierByIdAsync(int id)
		{
			var result = await _supplierService.GetSupplierByIdAsync(id);
			return HandleResponse(result);
		}

		[HttpPost]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> CreateSupplierAsync([FromBody] CreateSupplierRequest request)
		{
			var result = await _supplierService.CreateSupplierAsync(request);
			return HandleResponse(result);
		}

		[HttpPut("{id}")]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<SupplierResponse>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<SupplierResponse>>> UpdateSupplierAsync(int id, [FromBody] UpdateSupplierRequest request)
		{
			var result = await _supplierService.UpdateSupplierAsync(id, request);
			return HandleResponse(result);
		}

		[HttpDelete("{id}")]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status404NotFound)]
		public async Task<ActionResult<BaseResponse<bool>>> DeleteSupplierAsync(int id)
		{
			var result = await _supplierService.DeleteSupplierAsync(id);
			return HandleResponse(result);
		}
	}
}
