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
		public decimal? MaxDiscountAmount { get; private set; }
		public decimal MinOrderValue { get; private set; }
		public DateTime ExpiryDate { get; private set; }
		public int? RemainingQuantity { get; private set; }
		public int? MaxUsagePerUser { get; private set; }
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
		public static Voucher CreateRegular(VoucherRegularCreationFactor details)
		{
			ValidateCore(
				details.Code,
				details.DiscountValue,
				details.RequiredPoints,
				details.MaxDiscountAmount,
				details.MinOrderValue,
				details.ExpiryDate,
				details.TotalQuantity,
				details.MaxUsagePerUser);

			return new Voucher
			{
				Code = details.Code.Trim().ToUpperInvariant(),
				DiscountValue = details.DiscountValue,
				DiscountType = details.DiscountType,
				ApplyType = details.ApplyType,
				RequiredPoints = details.RequiredPoints,
				MaxDiscountAmount = details.MaxDiscountAmount,
				MinOrderValue = details.MinOrderValue,
				ExpiryDate = details.ExpiryDate,
				TotalQuantity = details.TotalQuantity,
				RemainingQuantity = details.TotalQuantity,
				MaxUsagePerUser = details.MaxUsagePerUser,
				IsPublic = details.IsPublic
			};
		}

		public static Voucher CreateCampaign(VoucherCampaignConfigFactor details)
		{
			if (details.CampaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (string.IsNullOrWhiteSpace(details.Code))
				throw DomainException.BadRequest("Voucher code is required.");

			if (details.DiscountValue <= 0)
				throw DomainException.BadRequest("Discount value must be greater than 0.");

			if (details.ExpiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			return new Voucher
			{
				Code = details.Code.Trim().ToUpperInvariant(),
				DiscountValue = details.DiscountValue,
				DiscountType = details.DiscountType,
				ApplyType = details.ApplyType,
				TargetItemType = details.TargetItemType,
				CampaignId = details.CampaignId,
				ExpiryDate = details.ExpiryDate,
				IsPublic = true,
				RequiredPoints = 0,

				// Gán các giá trị từ details
				MinOrderValue = details.MinOrderValue,
				TotalQuantity = details.TotalQuantity,
				RemainingQuantity = details.TotalQuantity,
				MaxUsagePerUser = details.MaxUsagePerUser,
				MaxDiscountAmount = details.MaxDiscountAmount
			};
		}

		public void UpdateRegular(VoucherRegularUpdateFactor details)
		{
			ValidateCore(
				details.Code,
				details.DiscountValue,
				details.RequiredPoints,
			   details.MaxDiscountAmount,
				details.MinOrderValue,
				details.ExpiryDate,
			 details.TotalQuantity,
				details.MaxUsagePerUser);

			if (details.RemainingQuantity < 0)
				throw DomainException.BadRequest("Remaining quantity must be greater than or equal to 0.");

			if (details.RemainingQuantity > details.TotalQuantity)
				throw DomainException.BadRequest("Remaining quantity cannot exceed total quantity.");

			Code = details.Code.Trim().ToUpperInvariant();
			DiscountValue = details.DiscountValue;
			DiscountType = details.DiscountType;
			ApplyType = details.ApplyType;
			RequiredPoints = details.RequiredPoints;
			MaxDiscountAmount = details.MaxDiscountAmount;
			MinOrderValue = details.MinOrderValue;
			ExpiryDate = details.ExpiryDate;
			TotalQuantity = details.TotalQuantity;
			RemainingQuantity = details.RemainingQuantity;
			MaxUsagePerUser = details.MaxUsagePerUser;
			IsPublic = details.IsPublic;
		}

		public void UpdateCampaign(VoucherCampaignConfigFactor details)
		{
			if (details.CampaignId == Guid.Empty)
				throw DomainException.BadRequest("Campaign ID is required.");

			if (string.IsNullOrWhiteSpace(details.Code))
				throw DomainException.BadRequest("Voucher code is required.");

			if (details.DiscountValue <= 0)
				throw DomainException.BadRequest("Discount value must be greater than 0.");

			if (details.ExpiryDate <= DateTime.UtcNow)
				throw DomainException.BadRequest("Expiry date must be in the future.");

			Code = details.Code.Trim().ToUpperInvariant();
			DiscountValue = details.DiscountValue;
			DiscountType = details.DiscountType;
			ApplyType = details.ApplyType;
			TargetItemType = details.TargetItemType;
			CampaignId = details.CampaignId;
			ExpiryDate = details.ExpiryDate;
			IsPublic = true;
			RequiredPoints = 0;
			MinOrderValue = details.MinOrderValue;
			TotalQuantity = details.TotalQuantity;
			RemainingQuantity = details.TotalQuantity;
			MaxUsagePerUser = details.MaxUsagePerUser;
			MaxDiscountAmount = details.MaxDiscountAmount;
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
			if (RemainingQuantity.HasValue && RemainingQuantity.Value <= 0)
				throw DomainException.BadRequest("Voucher is out of stock");
		}

		public void DecreaseRemainingQuantity(int quantity = 1)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			EnsureInStock();

			if (!RemainingQuantity.HasValue)
				return;

			RemainingQuantity = Math.Max(0, RemainingQuantity.Value - quantity);
		}

		public void IncreaseRemainingQuantity(int quantity = 1)
		{
			if (quantity <= 0)
				throw DomainException.BadRequest("Quantity must be greater than 0.");

			if (!RemainingQuantity.HasValue || !TotalQuantity.HasValue)
				return;

			RemainingQuantity += quantity;

			if (RemainingQuantity > TotalQuantity.Value)
				RemainingQuantity = TotalQuantity.Value;
		}

		private static void ValidateCore(
			string code,
			decimal discountValue,
			int requiredPoints,
			decimal? maxDiscountAmount,
			decimal minOrderValue,
			DateTime expiryDate,
			int totalQuantity,
			int? maxUsagePerUser)
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

			if (maxDiscountAmount.HasValue && maxDiscountAmount.Value <= 0)
				throw DomainException.BadRequest("Max discount amount must be greater than 0.");

			if (maxUsagePerUser.HasValue && maxUsagePerUser.Value <= 0)
				throw DomainException.BadRequest("Max usage per user must be greater than 0.");
		}

		// Records
		public sealed record VoucherRegularCreationFactor
		{
			public required string Code { get; init; }
			public required decimal DiscountValue { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required VoucherType ApplyType { get; init; }
			public required int RequiredPoints { get; init; }
			public decimal? MaxDiscountAmount { get; init; }
			public required decimal MinOrderValue { get; init; }
			public required DateTime ExpiryDate { get; init; }
			public required int TotalQuantity { get; init; }
			public int? MaxUsagePerUser { get; init; }
			public required bool IsPublic { get; init; }
		}

		public sealed record VoucherRegularUpdateFactor
		{
			public required string Code { get; init; }
			public required decimal DiscountValue { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required VoucherType ApplyType { get; init; }
			public required int RequiredPoints { get; init; }
			public decimal? MaxDiscountAmount { get; init; }
			public required decimal MinOrderValue { get; init; }
			public required DateTime ExpiryDate { get; init; }
			public required int TotalQuantity { get; init; }
			public required int RemainingQuantity { get; init; }
			public int? MaxUsagePerUser { get; init; }
			public required bool IsPublic { get; init; }
		}

		public sealed record VoucherCampaignConfigFactor
		{
			public required string Code { get; init; }
			public required decimal DiscountValue { get; init; }
			public required DiscountType DiscountType { get; init; }
			public required VoucherType ApplyType { get; init; }
			public required PromotionType TargetItemType { get; init; }
			public required Guid CampaignId { get; init; }
			public required DateTime ExpiryDate { get; init; }

			// Thêm các thuộc tính này
			public decimal? MaxDiscountAmount { get; init; }
			public required decimal MinOrderValue { get; init; }
			public int? TotalQuantity { get; init; }
			public int? MaxUsagePerUser { get; init; }
		}
	}
}

