using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.API.Controllers.Base;
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

		/// <summary>
		/// Get supplier lookup list
		/// </summary>
		[HttpGet("lookup")]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierLookupItem>>), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(BaseResponse<List<SupplierLookupItem>>), StatusCodes.Status500InternalServerError)]
		public async Task<ActionResult<BaseResponse<List<SupplierLookupItem>>>> GetSupplierLookupList()
		{
			var result = await _supplierService.GetSupplierLookupListAsync();
			return HandleResponse(result);
		}
	}
}
