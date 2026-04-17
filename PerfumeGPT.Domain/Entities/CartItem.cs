using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class CartItem : BaseEntity<Guid>
	{
		protected CartItem() { }

		public Guid UserId { get; private set; }
		public Guid VariantId { get; private set; }
		public int Quantity { get; private set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;

		// Factory methods
		public static CartItem Create(Guid userId, Guid variantId, int quantity)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID là bắt buộc.");

			if (variantId == Guid.Empty)
				throw DomainException.BadRequest("Variant ID là bắt buộc.");

			if (quantity <= 0)
				throw DomainException.BadRequest("Số lượng phải lớn hơn 0.");

			return new CartItem
			{
				UserId = userId,
				VariantId = variantId,
				Quantity = quantity
			};
		}

		// Business logic methods
		public void SetQuantity(int quantity)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Số lượng phải lớn hơn 0.");

			Quantity = quantity;
		}

		public bool IsOwnedBy(Guid userId) => UserId == userId;
	}
}
