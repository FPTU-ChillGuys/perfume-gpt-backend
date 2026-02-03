using PerfumeGPT.Application.DTOs.Requests.Variants;
using PerfumeGPT.Application.DTOs.Responses.Variants;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVariantRepository : IGenericRepository<ProductVariant>
	{
		/// <summary>
		/// Gets a variant by barcode.
		/// </summary>
		Task<ProductVariantResponse?> GetByBarcodeAsync(string barcode);

		/// <summary>
		/// Gets a variant with concentration details.
		/// </summary>
		Task<ProductVariantResponse?> GetVariantWithDetailsAsync(Guid variantId);

		/// <summary>
		/// Gets paged variants with concentration details.
		/// </summary>
		Task<(List<VariantPagedItem> Items, int TotalCount)> GetPagedVariantsWithDetailsAsync(GetPagedVariantsRequest request);
	}
}

