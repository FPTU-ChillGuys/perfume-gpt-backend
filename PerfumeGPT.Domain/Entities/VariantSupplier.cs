using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class VariantSupplier : BaseEntity<Guid>, IHasTimestamps
	{
		private VariantSupplier() { }
		public Guid ProductVariantId { get; private set; }
		public int SupplierId { get; private set; }

		// Giá nhập ĐÃ THƯƠNG LƯỢNG hiện tại (AI sẽ lấy giá này)
		public decimal NegotiatedPrice { get; private set; }

		// Nếu 1 sản phẩm có nhiều nhà cung cấp, AI sẽ ưu tiên chọn nhà cung cấp Primary
		public bool IsPrimary { get; private set; }

		// Có thể thêm LeadTime (Số ngày giao hàng dự kiến) để AI tính toán ExpectedArrivalDate
		public int EstimatedLeadTimeDays { get; private set; }

		// Navigation Properties
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Supplier Supplier { get; set; } = null!;

		// IHasTimestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		public static VariantSupplier Create(Guid productVariantId, int supplierId, decimal negotiatedPrice, int leadTimeDays, bool isPrimary)
		{
			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant is required.");

			if (supplierId <= 0)
				throw DomainException.BadRequest("Supplier is required.");

			if (negotiatedPrice <= 0)
				throw DomainException.BadRequest("Price must be positive.");

			if (leadTimeDays < 0)
				throw DomainException.BadRequest("Lead time cannot be negative.");

			return new VariantSupplier
			{
				ProductVariantId = productVariantId,
				SupplierId = supplierId,
				NegotiatedPrice = negotiatedPrice,
				EstimatedLeadTimeDays = leadTimeDays,
				IsPrimary = isPrimary
			};
		}

		public void UpdatePricing(decimal newPrice, int leadTimeDays)
		{
			if (newPrice <= 0)
				throw DomainException.BadRequest("Price must be positive.");

			if (leadTimeDays < 0)
				throw DomainException.BadRequest("Lead time cannot be negative.");

			NegotiatedPrice = newPrice;
			EstimatedLeadTimeDays = leadTimeDays;
		}

		public void SetAsPrimary() => IsPrimary = true;
		public void RemovePrimary() => IsPrimary = false;
	}
}
