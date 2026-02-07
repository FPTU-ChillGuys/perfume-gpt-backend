using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVariantRepository : IGenericRepository<ProductVariant>
	{
		Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode);
		Task<ProductVariant?> GetBySkuAsync(string sku);
		Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId);
		Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(GetPagedVariantsRequest request);
		Task<List<Guid>> GetExistingIdsAsync(List<Guid> ids);
	}
}

