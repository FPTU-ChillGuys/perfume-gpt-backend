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
		public decimal ApportionedDiscount { get; private set; }
		public string Snapshot { get; private set; } = null!;

		// TÍNH TOÁN ĐỘNG: Tổng tiền thực tế khách phải trả cho dòng này (Không lưu xuống DB)
		public decimal FinalTotal => (UnitPrice * Quantity) - ApportionedDiscount;

		// TÍNH TOÁN ĐỘNG: Đơn giá được phép hoàn trả cho 1 sản phẩm (Không lưu xuống DB)
		public decimal RefundableUnitPrice => Quantity > 0 ? FinalTotal / Quantity : 0;

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Review? Review { get; set; }
		public virtual ICollection<OrderReturnRequestDetail> ReturnRequestDetails { get; set; } = [];

		// Factory methods
		public static OrderDetail Create(Guid variantId, int quantity, decimal unitPrice, string snapshot, decimal apportionedDiscount = 0)
		{
			if (variantId == Guid.Empty)
				throw DomainException.BadRequest("Variant ID is required.");

			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			if (unitPrice < 0)
				throw DomainException.BadRequest("Unit price cannot be negative.");

			if (apportionedDiscount < 0 || apportionedDiscount > (unitPrice * quantity))
				throw DomainException.BadRequest("Apportioned discount cannot be negative or exceed the total line price.");

			if (string.IsNullOrWhiteSpace(snapshot))
				throw DomainException.BadRequest("Snapshot is required.");

			return new OrderDetail
			{
				VariantId = variantId,
				Quantity = quantity,
				UnitPrice = unitPrice,
				ApportionedDiscount = apportionedDiscount,
				Snapshot = snapshot.Trim()
			};
		}

		public void ApplyDiscount(decimal discountAmount)
		{
			if (discountAmount < 0 || discountAmount > (UnitPrice * Quantity))
				throw DomainException.BadRequest("Invalid discount amount. It cannot be negative or exceed the total line price.");

			ApportionedDiscount = discountAmount;
		}
	}
}
