using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderDetail : BaseEntity<Guid>
	{
		protected OrderDetail() { }

		public Guid OrderId { get; private set; }
		public Guid VariantId { get; private set; }
		public int Quantity { get; private set; }
		public decimal UnitPrice { get; private set; }
		public string Snapshot { get; private set; } = null!;

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Review? Review { get; set; }
		public virtual ICollection<OrderReturnRequestDetail> ReturnRequestDetails { get; set; } = [];

		// Factory methods
		public static OrderDetail Create(Guid variantId, int quantity, decimal unitPrice, string snapshot)
		{
			if (variantId == Guid.Empty)
				throw DomainException.BadRequest("Variant ID is required.");

			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			if (unitPrice < 0)
				throw DomainException.BadRequest("Unit price cannot be negative.");

			if (string.IsNullOrWhiteSpace(snapshot))
				throw DomainException.BadRequest("Snapshot is required.");

			return new OrderDetail
			{
				VariantId = variantId,
				Quantity = quantity,
				UnitPrice = unitPrice,
				Snapshot = snapshot.Trim()
			};
		}
	}
}
