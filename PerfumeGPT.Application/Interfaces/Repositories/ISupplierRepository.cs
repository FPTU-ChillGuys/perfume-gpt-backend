using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Responses.Suppliers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ISupplierRepository : IGenericRepository<Supplier>
	{
		Task<List<SupplierLookupItem>> GetSupplierLookupListAsync();
		Task<List<SupplierResponse>> GetAllSuppliersAsync();
		Task<SupplierResponse?> GetSupplierByIdAsync(int id);
		Task<bool> HasImportTicketsAsync(int supplierId);
	}
}
