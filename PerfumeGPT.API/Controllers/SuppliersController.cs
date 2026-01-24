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

		[HttpGet("lookup")]
		public async Task<ActionResult<BaseResponse<List<SupplierLookupItem>>>> GetSupplierLookupList()
		{
			var result = await _supplierService.GetSupplierLookupListAsync();
			return HandleResponse(result);
		}
	}
}
