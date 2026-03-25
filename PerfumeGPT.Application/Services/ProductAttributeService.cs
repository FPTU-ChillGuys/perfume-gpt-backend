using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace PerfumeGPT.Application.Services
{
	public class ProductAttributeService : IProductAttributeService
	{
		#region Dependencies
		private readonly IAttributeRepository _attributeRepo;
		private readonly IAttributeValueRepository _attributeValueRepo;
		private readonly IProductAttributeRepository _productAttributeRepo;

		public ProductAttributeService(
			IAttributeRepository attributeRepo,
			IAttributeValueRepository attributeValueRepo,
			IProductAttributeRepository productAttributeRepo)
		{
			_attributeRepo = attributeRepo;
			_attributeValueRepo = attributeValueRepo;
			_productAttributeRepo = productAttributeRepo;
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
			var existingAttributes = await _attributeRepo.GetExistingIdsAsync(attributeIds);
			var missingAttributes = attributeIds.Except(existingAttributes).ToList();
			if (missingAttributes.Count != 0)
			{
				errors.Add($"Attribute(s) not found: {string.Join(", ", missingAttributes)}");
			}

			var existingValues = await _attributeValueRepo.GetExistingIdsAsync(valueIds);
			var missingValues = valueIds.Except(existingValues).ToList();
			if (missingValues.Count != 0)
			{
				errors.Add($"Attribute value(s) not found: {string.Join(", ", missingValues)}");
			}

			// Load attribute entities once for level checks
			var attributesEntities = await _attributeRepo.GetByIdsAsync(attributeIds);

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

			// Fetch attribute values in batch to avoid per-item DB calls
			var valueEntities = await _attributeValueRepo.Query()
				.Where(v => valueIds.Contains(v.Id))
				.ToDictionaryAsync(v => v.Id);

			// Validate relation between attribute and value and duplicates
			var seenPairs = new HashSet<(int AttributeId, int ValueId)>();
			foreach (var attr in attributes)
			{
				if (!valueEntities.TryGetValue(attr.ValueId, out var val) || val.AttributeId != attr.AttributeId)
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

		public void ApplyAttributesToProductEntity(Product product, List<ProductAttributeDto>? attributes)
		{
			if (attributes == null || attributes.Count == 0) return;

			product.ProductAttributes ??= [];
			foreach (var a in attributes)
			{
				product.ProductAttributes.Add(ProductAttribute.Create(a.AttributeId, a.ValueId));
			}
		}

		public void ApplyAttributesToVariantEntity(ProductVariant variant, List<ProductAttributeDto>? attributes)
		{
			if (attributes == null || attributes.Count == 0) return;

			variant.ProductAttributes ??= [];
			foreach (var a in attributes)
			{
				variant.ProductAttributes.Add(ProductAttribute.Create(a.AttributeId, a.ValueId));
			}
		}

		public async Task ReplaceAttributesAsync(Guid entityId, List<ProductAttributeDto>? attributes, bool isVariant = false)
		{
			var existing = isVariant
				? await _productAttributeRepo.GetByVariantIdAsync(entityId)
				: await _productAttributeRepo.GetByProductIdAsync(entityId);

			if (existing.Count != 0)
			{
				_productAttributeRepo.RemoveRange(existing);
			}

			if (attributes != null && attributes.Count > 0)
			{
				var newEntities = attributes
					 .Select(a => isVariant
						 ? ProductAttribute.CreateForVariant(entityId, a.AttributeId, a.ValueId)
						 : ProductAttribute.CreateForProduct(entityId, a.AttributeId, a.ValueId))
					 .ToList();

				await _productAttributeRepo.AddRangeAsync(newEntities);
			}
		}

		public async Task RemoveAttributesByEntityIdAsync(Guid entityId, bool isVariant = false)
		{
			var existing = isVariant
				? await _productAttributeRepo.GetByVariantIdAsync(entityId)
				: await _productAttributeRepo.GetByProductIdAsync(entityId);

			if (existing.Count != 0)
			{
				_productAttributeRepo.RemoveRange(existing);
			}
		}
	}
}
