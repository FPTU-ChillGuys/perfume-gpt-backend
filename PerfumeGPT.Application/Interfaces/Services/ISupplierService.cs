using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Suppliers;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ISupplierService
	{
		Task<BaseResponse<List<SupplierLookupItem>>> GetSupplierLookupListAsync();
	}
}
