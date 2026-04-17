using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class OrderDetail : BaseEntity<Guid>
	{
		protected OrderDetail() { }

		public Guid OrderId { get; private set; }
		public Guid VariantId { get; private set; }
		public Guid? PromotionItemId { get; private set; }
		public Guid? FulfilledBatchId { get; private set; }
		public int Quantity { get; private set; }
		public decimal UnitPrice { get; private set; }
		public decimal PromotionDiscountAmount { get; private set; }
		public decimal ApportionedDiscount { get; private set; }
		public string Snapshot { get; private set; } = null!;
		public decimal FinalTotal => (UnitPrice * Quantity) - ApportionedDiscount - PromotionDiscountAmount;
		public decimal RefundableUnitPrice => Quantity > 0 ? FinalTotal / Quantity : 0;

		// Navigation properties
		public virtual Order Order { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual Review? Review { get; set; }
		public virtual Batch? FulfilledBatch { get; set; } = null!;
		public virtual PromotionItem? PromotionItem { get; set; }
		public virtual ICollection<OrderReturnRequestDetail> ReturnRequestDetails { get; set; } = [];

		// Factory methods
		public static OrderDetail Create(
		Guid variantId,
		Guid? promotionItemId, // Thêm tham số này
		int quantity,
		decimal unitPrice,
		string snapshot,
		decimal apportionedVoucherDiscount = 0, // Thêm tham số này
		decimal promotionDiscountAmount = 0)
		{
			if (variantId == Guid.Empty)
                throw DomainException.BadRequest("Variant ID là bắt buộc.");

			if (quantity <= 0)
               throw DomainException.BadRequest("Số lượng phải lớn hơn 0.");

			if (unitPrice < 0)
             throw DomainException.BadRequest("Đơn giá không được âm.");

			if (apportionedVoucherDiscount + promotionDiscountAmount > (unitPrice * quantity))
                throw DomainException.BadRequest("Tổng giảm giá không được vượt quá tổng tiền dòng sản phẩm.");

			if (string.IsNullOrWhiteSpace(snapshot))
              throw DomainException.BadRequest("Snapshot là bắt buộc.");

			return new OrderDetail
			{
				VariantId = variantId,
				PromotionItemId = promotionItemId,
				Quantity = quantity,
				UnitPrice = unitPrice,
				ApportionedDiscount = apportionedVoucherDiscount,
				PromotionDiscountAmount = promotionDiscountAmount,
				Snapshot = snapshot.Trim()
			};
		}

		public void ApplyDiscounts(decimal apportionedVoucherDiscount, decimal promotionDiscountAmount)
		{
			if (apportionedVoucherDiscount + promotionDiscountAmount > (UnitPrice * Quantity))
                throw DomainException.BadRequest("Tổng giảm giá không được vượt quá tổng tiền dòng sản phẩm.");

			ApportionedDiscount = apportionedVoucherDiscount;
			PromotionDiscountAmount = promotionDiscountAmount;
		}

		public void Fulfill(Guid actualBatchId)
		{
			if (actualBatchId == Guid.Empty)
               throw DomainException.BadRequest("Actual Batch ID là bắt buộc khi hoàn tất đơn.");

			FulfilledBatchId = actualBatchId;
		}

		public void UpdateQuantityAndDiscount(int newQuantity, decimal newApportionedDiscount)
		{
			if (newQuantity <= 0)
                throw DomainException.BadRequest("Số lượng phải lớn hơn 0.");

			Quantity = newQuantity;
			ApportionedDiscount = newApportionedDiscount;
		}
	}
}
