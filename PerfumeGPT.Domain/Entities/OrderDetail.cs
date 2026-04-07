using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;
using System.Text.Json.Nodes;

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
		public decimal FinalTotal => (UnitPrice * Quantity) - ApportionedDiscount;
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

		public void UpdateBatchInfoInSnapshot(Guid newBatchId, string newBatchCode, DateTime newExpiryDate)
		{
			if (string.IsNullOrWhiteSpace(Snapshot))
				throw DomainException.BadRequest("Cannot update an empty snapshot.");

			var snapshotNode = JsonNode.Parse(Snapshot) ?? throw DomainException.BadRequest("Failed to parse snapshot JSON.");

			snapshotNode["BatchId"] = newBatchId;
			snapshotNode["BatchCode"] = newBatchCode;
			snapshotNode["ExpiryDate"] = newExpiryDate;

			Snapshot = snapshotNode.ToJsonString();
		}

		public void UpdateQuantityAndDiscount(int newQuantity, decimal newApportionedDiscount)
		{
			if (newQuantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than zero.");

			Quantity = newQuantity;
			ApportionedDiscount = newApportionedDiscount;
		}
	}
}
