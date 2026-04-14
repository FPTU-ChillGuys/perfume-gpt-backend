using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVariantRepository : IGenericRepository<ProductVariant>
	{
		Task<List<VariantLookupItem>> GetLookupList(Guid? productId = null);
		Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode);
		Task<ProductVariantForPosResponse?> GetVariantByInfoAsync(string keyword);
		Task<ProductVariant?> GetBySkuAsync(string sku);
		Task<ProductVariant?> GetByIdWithAttributesAsync(Guid variantId);
		Task<ProductVariant?> GetByIdWithSuppliersAsync(Guid variantId);
		Task<List<ProductVariant>> GetVariantsWithDetailsByIdsAsync(IEnumerable<Guid> variantIds);
		Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId);
		Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(GetPagedVariantsRequest request);
		Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsByCampaignIdAsync(Guid campaignId, GetPagedVariantsRequest request);
		Task<List<Guid>> GetExistingIdsAsync(List<Guid> ids);
		Task<VariantCreateOrder?> GetVariantForCreateOrderAsync(Guid variantId);
	}
}

