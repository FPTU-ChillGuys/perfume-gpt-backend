using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ProductFamilyMap : BaseEntity<Guid>
	{
		protected ProductFamilyMap() { }

		public Guid ProductId { get; private set; }
		public int OlfactoryFamilyId { get; private set; }

		// Navigation properties
		public virtual Product Product { get; set; } = null!;
		public virtual OlfactoryFamily OlfactoryFamily { get; set; } = null!;

		// Factory methods
		public static ProductFamilyMap Create(int olfactoryFamilyId)
		{
			if (olfactoryFamilyId <= 0)
				throw DomainException.BadRequest("Olfactory family ID must be greater than 0.");

			return new ProductFamilyMap
			{
				OlfactoryFamilyId = olfactoryFamilyId
			};
		}

		public static ProductFamilyMap CreateForProduct(Guid productId, int olfactoryFamilyId)
		{
			if (productId == Guid.Empty)
				throw DomainException.BadRequest("Product ID is required.");

			var map = Create(olfactoryFamilyId);
			map.ProductId = productId;
			return map;
		}
	}
}
