using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IProductAttributeService
	{
		Task<List<string>> ValidateAttributesAsync(List<ProductAttributeDto>? attributes, bool isForVariant = false);
		Task ReplaceAttributesAsync(Guid entityId, List<ProductAttributeDto>? attributes, bool isVariant = false);
		Task RemoveAttributesByEntityIdAsync(Guid entityId, bool isVariant = false);
	}
}
