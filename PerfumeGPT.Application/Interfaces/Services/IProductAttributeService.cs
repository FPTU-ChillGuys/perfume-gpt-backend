using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IProductAttributeService
	{
		Task<List<string>> ValidateAttributesAsync(List<ProductAttributeDto>? attributes, bool isForVariant = false);
		void ApplyAttributesToProductEntity(Product product, List<ProductAttributeDto>? attributes);
		void ApplyAttributesToVariantEntity(ProductVariant variant, List<ProductAttributeDto>? attributes);
		Task ReplaceAttributesAsync(Guid entityId, List<ProductAttributeDto>? attributes, bool isVariant = false);
		Task RemoveAttributesByEntityIdAsync(Guid entityId, bool isVariant = false);
	}
}
