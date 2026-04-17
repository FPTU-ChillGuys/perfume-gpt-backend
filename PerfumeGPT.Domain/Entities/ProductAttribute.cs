using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductAttribute : BaseEntity<Guid>
	{
		protected ProductAttribute() { }

		public Guid? ProductId { get; private set; }
		public Guid? VariantId { get; private set; }
		public int AttributeId { get; private set; }
		public int ValueId { get; private set; }

		// Navigation properties
		public virtual Product? Product { get; set; }
		public virtual ProductVariant? Variant { get; set; }
		public virtual Attribute Attribute { get; set; } = null!;
		public virtual AttributeValue Value { get; set; } = null!;

		// Factory methods
		public static ProductAttribute Create(int attributeId, int valueId)
		{
			ValidateCore(attributeId, valueId);

			return new ProductAttribute
			{
				AttributeId = attributeId,
				ValueId = valueId
			};
		}

		public static ProductAttribute CreateForProduct(Guid productId, int attributeId, int valueId)
		{
			if (productId == Guid.Empty)
                throw DomainException.BadRequest("Product ID là bắt buộc.");

			ValidateCore(attributeId, valueId);

			return new ProductAttribute
			{
				ProductId = productId,
				AttributeId = attributeId,
				ValueId = valueId
			};
		}

		public static ProductAttribute CreateForVariant(Guid variantId, int attributeId, int valueId)
		{
			if (variantId == Guid.Empty)
                throw DomainException.BadRequest("Variant ID là bắt buộc.");

			ValidateCore(attributeId, valueId);

			return new ProductAttribute
			{
				VariantId = variantId,
				AttributeId = attributeId,
				ValueId = valueId
			};
		}

		private static void ValidateCore(int attributeId, int valueId)
		{
			if (attributeId <= 0)
               throw DomainException.BadRequest("Attribute ID phải lớn hơn 0.");

			if (valueId <= 0)
               throw DomainException.BadRequest("Value ID phải lớn hơn 0.");
		}
	}
}
