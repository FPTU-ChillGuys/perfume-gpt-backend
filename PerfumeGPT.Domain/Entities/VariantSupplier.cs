using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class VariantSupplier : BaseEntity<Guid>, IHasTimestamps
	{
		internal VariantSupplier() { }
		public Guid ProductVariantId { get; internal set; }
		public int SupplierId { get; internal set; }

		// Giá nhập ĐÃ THƯƠNG LƯỢNG hiện tại (AI sẽ lấy giá này)
		public decimal NegotiatedPrice { get; internal set; }

		// Nếu 1 sản phẩm có nhiều nhà cung cấp, AI sẽ ưu tiên chọn nhà cung cấp Primary
		public bool IsPrimary { get; internal set; }

		// Có thể thêm LeadTime (Số ngày giao hàng dự kiến) để AI tính toán ExpectedArrivalDate
		public int EstimatedLeadTimeDays { get; internal set; }

		// Navigation Properties
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Supplier Supplier { get; set; } = null!;

		// IHasTimestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Business Logic
		internal void UpdatePriceAndLeadTime(decimal newPrice, int leadTimeDays)
		{
			if (newPrice <= 0) throw new DomainException("Price must be positive.");
			if (leadTimeDays < 0) throw new DomainException("Lead time cannot be negative.");

			NegotiatedPrice = newPrice;
			EstimatedLeadTimeDays = leadTimeDays;
		}

		internal void SetAsPrimary() => IsPrimary = true;
		internal void RemovePrimary() => IsPrimary = false;
	}
}
