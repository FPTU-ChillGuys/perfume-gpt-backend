using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class StockReservation : BaseEntity<Guid>, IHasTimestamps
	{
		protected StockReservation() { }

		public Guid OrderId { get; private set; }
		public Guid BatchId { get; private set; }
		public Guid VariantId { get; private set; }
		public int ReservedQuantity { get; private set; }
		public ReservationStatus Status { get; private set; }
		public DateTime? ExpiresAt { get; private set; }

		// Navigation properties
		public virtual Order Order { get; private set; } = null!;
		public virtual Batch Batch { get; private set; } = null!;
		public virtual ProductVariant ProductVariant { get; private set; } = null!;

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory method
		public StockReservation(Guid orderId, Guid batchId, Guid variantId, int quantity, DateTime? expiresAt)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Reservation quantity must be strictly positive.");

			OrderId = orderId;
			BatchId = batchId;
			VariantId = variantId;
			ReservedQuantity = quantity;
			Status = ReservationStatus.Reserved;
			ExpiresAt = expiresAt;
		}

		// Business logic methods
		public void Commit()
		{
			if (Status != ReservationStatus.Reserved)
				throw DomainException.Conflict($"Cannot commit reservation. Current status is {Status}.");

			Status = ReservationStatus.Committed;
		}

		public void Release()
		{
			if (Status != ReservationStatus.Reserved)
				throw DomainException.Conflict($"Cannot release reservation. Current status is {Status}.");

			Status = ReservationStatus.Released;
		}

		public void Restock()
		{
			if (Status != ReservationStatus.Committed)
				throw DomainException.Conflict($"Cannot restock reservation. Current status is {Status}.");

			Status = ReservationStatus.Released;
		}

		public void DecreaseQuantity(int quantity)
		{
			if (quantity <= 0 || quantity > ReservedQuantity)
				throw DomainException.BadRequest("Invalid decrease quantity.");
			ReservedQuantity -= quantity;
		}

		public void SetExpiration(DateTime? expiresAt)
		{
			ExpiresAt = expiresAt;
		}
	}
}
