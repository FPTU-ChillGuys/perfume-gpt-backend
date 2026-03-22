using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace PerfumeGPT.Domain.Entities
{
	public class Batch : BaseEntity<Guid>, IHasCreatedAt
	{
		public Guid VariantId { get; set; }
		public Guid ImportDetailId { get; set; }

		public string BatchCode { get; set; } = null!;
		public DateTime ManufactureDate { get; set; }
		public DateTime ExpiryDate { get; set; }
		public int ImportQuantity { get; set; }
		public int RemainingQuantity { get; set; }
		public int ReservedQuantity { get; set; }
		public int AvailableInBatch => RemainingQuantity - ReservedQuantity;

		[Timestamp]
		public byte[] RowVersion { get; set; } = null!;

		// Navigation Properties
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ImportDetail ImportDetail { get; set; } = null!;
		public virtual ICollection<StockAdjustmentDetail> StockAdjustmentDetails { get; set; } = [];
		public virtual ICollection<StockReservation> StockReservations { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<PromotionItem> Promotions { get; set; } = [];

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Business Logic
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

		public bool CanIncreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity + quantity <= ImportQuantity;

		public bool CanDecreaseQuantity(int quantity)
			=> quantity > 0 && RemainingQuantity >= quantity;

		public void IncreaseQuantity(int quantity)
		{
			if (!CanIncreaseQuantity(quantity))
				throw DomainException.BadRequest("Cannot increase batch quantity beyond import quantity.");

			RemainingQuantity += quantity;
		}

		public void DecreaseQuantity(int quantity)
		{
			if (!CanDecreaseQuantity(quantity))
				throw DomainException.BadRequest("Cannot decrease batch quantity below zero.");

			RemainingQuantity -= quantity;
		}
	}
}
