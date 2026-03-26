using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface ISupplierService
	{
		Task<BaseResponse<List<SupplierLookupItem>>> GetSupplierLookupListAsync();
		Task<BaseResponse<SupplierResponse>> GetSupplierByIdAsync(int id);
		Task<BaseResponse<List<SupplierResponse>>> GetAllSuppliersAsync();
		Task<BaseResponse<SupplierResponse>> CreateSupplierAsync(CreateSupplierRequest request);
		Task<BaseResponse<SupplierResponse>> UpdateSupplierAsync(int id, UpdateSupplierRequest request);
		Task<BaseResponse<bool>> DeleteSupplierAsync(int id);
	}
}
