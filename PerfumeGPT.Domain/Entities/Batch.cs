using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Batch : BaseEntity<Guid>, IHasCreatedAt
	{
		private Batch() { }

		public Guid VariantId { get; private set; }
		public Guid ImportDetailId { get; private set; }
		public string BatchCode { get; private set; } = null!;
		public DateTime ManufactureDate { get; private set; }
		public DateTime ExpiryDate { get; private set; }
		public int ImportQuantity { get; private set; }
		public int RemainingQuantity { get; private set; }
		public int ReservedQuantity { get; private set; }
		public int AvailableInBatch => RemainingQuantity - ReservedQuantity;

		[Timestamp]
		public byte[] RowVersion { get; set; } = null!;

		// Navigation properties
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ImportDetail ImportDetail { get; set; } = null!;
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<PromotionItem> Promotions { get; set; } = [];

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Batch CreateForImport(
			Guid variantId,
			Guid importDetailId,
			string batchCode,
			DateTime manufactureDate,
			DateTime expiryDate,
			int quantity)
		{
			if (string.IsNullOrWhiteSpace(batchCode))
				throw DomainException.BadRequest("Batch code is required.");

			if (quantity <= 0)
				throw DomainException.BadRequest("Batch quantity must be greater than 0.");

			if (expiryDate <= manufactureDate)
				throw DomainException.BadRequest("Expiry date must be later than manufacture date.");

			if (expiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			return new Batch
			{
				VariantId = variantId,
				ImportDetailId = importDetailId,
				BatchCode = batchCode.Trim(),
				ManufactureDate = manufactureDate,
				ExpiryDate = expiryDate,
				ImportQuantity = quantity,
				RemainingQuantity = quantity,
				ReservedQuantity = 0
			};
		}

		// Business logic methods
		public bool CanIncreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity + quantity <= ImportQuantity;

		public bool CanDecreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity >= quantity;

		public void IncreaseQuantity(int quantity)
		{
			if (!CanIncreaseQuantity(quantity))
				throw DomainException.BadRequest(
					$"Cannot increase quantity beyond import quantity of {ImportQuantity}.");
			RemainingQuantity += quantity;
		}

		public void DecreaseQuantity(int quantity)
		{
			if (!CanDecreaseQuantity(quantity))
				throw DomainException.BadRequest(
					"Cannot decrease batch quantity below zero.");
			RemainingQuantity -= quantity;
		}

		public void Reserve(int quantity)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Reserve quantity must be greater than 0.");
			if (AvailableInBatch < quantity)
				throw DomainException.BadRequest("Insufficient available quantity to reserve.");
			ReservedQuantity += quantity;
		}

		public void Release(int quantity)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Release quantity must be greater than 0.");
			if (ReservedQuantity < quantity)
				throw DomainException.BadRequest("Cannot release more than reserved quantity.");
			ReservedQuantity -= quantity;
		}
	}
}
