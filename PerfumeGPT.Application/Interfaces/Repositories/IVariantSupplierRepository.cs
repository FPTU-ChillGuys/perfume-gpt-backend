using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVariantSupplierRepository : IGenericRepository<VariantSupplier>
	{
		Task<List<CatalogItemResponse>> GetCatalogsAsync(int? supplierId, Guid? variantId);
		Task<List<VariantSupplier>> GetByVariantIdAsync(Guid variantId);
		Task<VariantSupplier?> GetByVariantAndSupplierAsync(Guid variantId, int supplierId);
	}
}
