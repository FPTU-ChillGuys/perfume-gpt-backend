using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Voucher : BaseEntity<Guid>, IHasTimestamps, ISoftDelete
	{
		protected Voucher() { }

		public string Code { get; private set; } = null!;
		public decimal DiscountValue { get; private set; }
		public DiscountType DiscountType { get; private set; } // Percentage, FixedAmount

		// Campaign properties
		public Guid? CampaignId { get; private set; }
		public VoucherType ApplyType { get; private set; } // OrderLevel, ProductLevel
		public PromotionType? TargetItemType { get; private set; } // Clearance, NewArrival, Regular, etc.

		// Redemption properties
		public int RequiredPoints { get; private set; }
		public decimal MinOrderValue { get; private set; }
		public DateTime ExpiryDate { get; private set; }
		public int? RemainingQuantity { get; private set; }
		public int? TotalQuantity { get; private set; }
		public bool IsPublic { get; private set; }

		// Navigation properties
		public virtual Campaign? Campaign { get; set; }
		public virtual ICollection<UserVoucher> UserVouchers { get; set; } = [];
		public virtual ICollection<Notification> Notifications { get; set; } = [];
		public virtual ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = [];

		// ISoftDelete implementation
		public bool IsDeleted { get; set; }
		public DateTime? DeletedAt { get; set; }

		// IHasTimestamps implementation
		public DateTime? UpdatedAt { get; set; }
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Voucher CreateRegular(
			string code,
			decimal discountValue,
			DiscountType discountType,
			VoucherType applyType,
			int requiredPoints,
			decimal minOrderValue,
			DateTime expiryDate,
			int totalQuantity,
			bool isPublic)
		{
			ValidateCore(code, discountValue, requiredPoints, minOrderValue, expiryDate, totalQuantity);

			return new Voucher
			{
				Code = code.Trim().ToUpperInvariant(),
				DiscountValue = discountValue,
				DiscountType = discountType,
				ApplyType = applyType,
				RequiredPoints = requiredPoints,
				MinOrderValue = minOrderValue,
				ExpiryDate = expiryDate,
				TotalQuantity = totalQuantity,
				RemainingQuantity = totalQuantity,
				IsPublic = isPublic
			};
		}

		public static Voucher CreateCampaign(
			string code,
			decimal discountValue,
			DiscountType discountType,
			VoucherType applyType,
			PromotionType targetItemType,
			Guid campaignId,
			DateTime expiryDate)
		{
			if (campaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (string.IsNullOrWhiteSpace(code))
				throw DomainException.BadRequest("Voucher code is required.");

			if (discountValue <= 0)
				throw DomainException.BadRequest("Discount value must be greater than 0.");

			if (expiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			return new Voucher
			{
				Code = code.Trim().ToUpperInvariant(),
				DiscountValue = discountValue,
				DiscountType = discountType,
				ApplyType = applyType,
				TargetItemType = targetItemType,
				CampaignId = campaignId,
				ExpiryDate = expiryDate,
				IsPublic = true,
				RequiredPoints = 0,
				MinOrderValue = 0,
				TotalQuantity = null,
				RemainingQuantity = null
			};
		}

		public void UpdateRegular(
			string code,
			decimal discountValue,
			DiscountType discountType,
			VoucherType applyType,
			int requiredPoints,
			decimal minOrderValue,
			DateTime expiryDate,
			int totalQuantity,
			int remainingQuantity,
			bool isPublic)
		{
			ValidateCore(code, discountValue, requiredPoints, minOrderValue, expiryDate, totalQuantity);

			if (remainingQuantity < 0)
				throw DomainException.BadRequest("Remaining quantity must be greater than or equal to 0.");

			if (remainingQuantity > totalQuantity)
				throw DomainException.BadRequest("Remaining quantity cannot exceed total quantity.");

			Code = code.Trim().ToUpperInvariant();
			DiscountValue = discountValue;
			DiscountType = discountType;
			ApplyType = applyType;
			RequiredPoints = requiredPoints;
			MinOrderValue = minOrderValue;
			ExpiryDate = expiryDate;
			TotalQuantity = totalQuantity;
			RemainingQuantity = remainingQuantity;
			IsPublic = isPublic;
		}

		public void UpdateCampaign(
			string code,
			decimal discountValue,
			DiscountType discountType,
			VoucherType applyType,
			PromotionType targetItemType,
			Guid campaignId,
			DateTime expiryDate)
		{
			if (campaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (string.IsNullOrWhiteSpace(code))
				throw DomainException.BadRequest("Voucher code is required.");

			if (discountValue <= 0)
				throw DomainException.BadRequest("Discount value must be greater than 0.");

			if (expiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			Code = code.Trim().ToUpperInvariant();
			DiscountValue = discountValue;
			DiscountType = discountType;
			ApplyType = applyType;
			TargetItemType = targetItemType;
			CampaignId = campaignId;
			ExpiryDate = expiryDate;
			IsPublic = true;
			RequiredPoints = 0;
			MinOrderValue = 0;
			TotalQuantity = null;
			RemainingQuantity = null;
		}

		// Business logic methods
		public void EnsureNotDeleted()
		{
			if (IsDeleted)
				throw DomainException.NotFound("Voucher not found");
		}

		public void EnsureNotExpired(DateTime nowUtc)
		{
			if (ExpiryDate < nowUtc)
				throw DomainException.BadRequest("Voucher has expired");
		}

		public void EnsureInStock()
		{
			if (!RemainingQuantity.HasValue || RemainingQuantity.Value <= 0)
				throw DomainException.BadRequest("Voucher is out of stock");
		}

		public void DecreaseRemainingQuantity(int quantity = 1)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			EnsureInStock();
			RemainingQuantity = Math.Max(0, RemainingQuantity!.Value - quantity);
		}

		public void IncreaseRemainingQuantity(int quantity = 1)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			if (!RemainingQuantity.HasValue)
				RemainingQuantity = 0;

			RemainingQuantity += quantity;

			if (TotalQuantity.HasValue && RemainingQuantity > TotalQuantity)
				RemainingQuantity = TotalQuantity;
		}

		private static void ValidateCore(
			string code,
			decimal discountValue,
			int requiredPoints,
			decimal minOrderValue,
			DateTime expiryDate,
			int totalQuantity)
		{
			if (string.IsNullOrWhiteSpace(code))
				throw DomainException.BadRequest("Voucher code is required.");

			if (discountValue <= 0)
				throw DomainException.BadRequest("Discount value must be greater than 0.");

			if (requiredPoints < 0)
				throw DomainException.BadRequest("Required points must be greater than or equal to 0.");

			if (minOrderValue < 0)
				throw DomainException.BadRequest("Minimum order value must be greater than or equal to 0.");

			if (expiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			if (totalQuantity <= 0)
				throw DomainException.BadRequest("Total quantity must be greater than 0.");
		}
	}
}

