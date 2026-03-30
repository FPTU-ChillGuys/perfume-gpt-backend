using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class ProductAttributeService : IProductAttributeService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;

		public ProductAttributeService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies

		public async Task<List<string>> ValidateAttributesAsync(List<ProductAttributeDto>? attributes, bool isForVariant = false)
		{
			var errors = new List<string>();
			if (attributes == null || attributes.Count == 0) return errors;

			// Gather distinct ids
			var attributeIds = attributes.Select(a => a.AttributeId).Distinct().ToList();
			var valueIds = attributes.Select(a => a.ValueId).Distinct().ToList();

			// Check existence in batch
			var existingAttributes = await _unitOfWork.Attributes.GetExistingIdsAsync(attributeIds);
			var missingAttributes = attributeIds.Except(existingAttributes).ToList();
			if (missingAttributes.Count != 0)
			{
				errors.Add($"Attribute(s) not found: {string.Join(", ", missingAttributes)}");
			}

			var existingValues = await _unitOfWork.AttributeValues.GetExistingIdsAsync(valueIds);
			var missingValues = valueIds.Except(existingValues).ToList();
			if (missingValues.Count != 0)
			{
				errors.Add($"Attribute value(s) not found: {string.Join(", ", missingValues)}");
			}

			// Load attribute entities once for level checks
			var attributesEntities = await _unitOfWork.Attributes.GetByIdsAsync(attributeIds);

			if (isForVariant)
			{
				// For variants: attributes must be variant-level
				var nonVariantAttrs = attributesEntities.Where(a => !a.IsVariantLevel).Select(a => a.Id).ToList();
				if (nonVariantAttrs.Count != 0)
				{
					errors.Add($"Attribute(s) are product-level and cannot be assigned to variant: {string.Join(", ", nonVariantAttrs)}");
				}
			}
			else
			{
				// For products: attributes must NOT be variant-level
				var variantLevelAttrs = attributesEntities.Where(a => a.IsVariantLevel).Select(a => a.Id).ToList();
				if (variantLevelAttrs.Count != 0)
				{
					errors.Add($"Attribute(s) are variant-level and cannot be assigned to product: {string.Join(", ", variantLevelAttrs)}");
				}
			}

			var valueAttributeMap = await _unitOfWork.AttributeValues.GetAttributeMapByValueIdsAsync(valueIds);

			// Validate relation between attribute and value and duplicates
			var seenPairs = new HashSet<(int AttributeId, int ValueId)>();
			foreach (var attr in attributes)
			{
				if (!valueAttributeMap.TryGetValue(attr.ValueId, out var valueAttributeId)
					|| valueAttributeId != attr.AttributeId)
				{
					errors.Add($"AttributeId {attr.AttributeId} and ValueId {attr.ValueId} mismatch");
					continue;
				}

				var pair = (attr.AttributeId, attr.ValueId);
				if (!seenPairs.Add(pair))
				{
					errors.Add($"Duplicate attribute-value pair: AttributeId {attr.AttributeId} ValueId {attr.ValueId}");
				}
			}

			return errors;
		}

		public async Task ReplaceAttributesAsync(Guid entityId, List<ProductAttributeDto>? attributes, bool isVariant = false)
		{
			var attributeData = attributes?.Select(a => (a.AttributeId, a.ValueId)) ?? [];

			if (isVariant)
			{
				var variant = await _unitOfWork.Variants.GetByIdWithAttributesAsync(entityId);
				if (variant == null) return;

				variant.SyncAttributes(attributeData);
				return;
			}

			var product = await _unitOfWork.Products.GetProductByIdWithAttributesAsync(entityId);
			if (product == null) return;

			product.SyncAttributes(attributeData);
		}

		public async Task RemoveAttributesByEntityIdAsync(Guid entityId, bool isVariant = false)
		{
			await ReplaceAttributesAsync(entityId, null, isVariant);
		}
	}
}
