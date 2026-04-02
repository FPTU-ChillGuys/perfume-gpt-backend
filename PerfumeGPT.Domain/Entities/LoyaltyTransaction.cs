using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class LoyaltyTransaction : BaseEntity<Guid>
	{
		protected LoyaltyTransaction() { }

		public Guid UserId { get; private set; }
		public Guid? VoucherId { get; private set; }
		public Guid? OrderId { get; private set; }
		public LoyaltyTransactionType TransactionType { get; private set; }
		public int PointsChanged { get; private set; }
		public string Reason { get; private set; } = null!;

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual Voucher? Voucher { get; set; }
		public virtual Order? Order { get; set; }

		// Factory methods
		public static LoyaltyTransaction CreateManual(Guid userId, ManualTransactionInfo info)
		{
			if (string.IsNullOrWhiteSpace(info.Reason))
				throw DomainException.BadRequest("Reason is required for manual loyalty point changes.");

			return info.TransactionType switch
			{
				LoyaltyTransactionType.Earn => CreateEarn(userId, new EarnTransactionInfo
				{
					Points = info.Points,
					Reason = info.Reason
				}),
				LoyaltyTransactionType.Spend => CreateSpend(userId, new SpendTransactionInfo
				{
					Points = info.Points,
					Reason = info.Reason
				}),
				_ => throw DomainException.BadRequest("Unsupported loyalty transaction type.")
			};
		}

		public static LoyaltyTransaction CreateEarn(Guid userId, EarnTransactionInfo info)
		{
			ValidateCreateArgs(userId, info.Points);

			return new LoyaltyTransaction
			{
				UserId = userId,
				OrderId = info.OrderId,
				TransactionType = LoyaltyTransactionType.Earn,
				PointsChanged = info.Points,
				Reason = string.IsNullOrWhiteSpace(info.Reason)
					? info.OrderId.HasValue ? $"Earned from Order {info.OrderId}" : "Manual point addition"
					: info.Reason.Trim()
			};
		}

		public static LoyaltyTransaction CreateSpend(Guid userId, SpendTransactionInfo info)
		{
			ValidateCreateArgs(userId, info.Points);

			return new LoyaltyTransaction
			{
				UserId = userId,
				VoucherId = info.VoucherId,
				OrderId = info.OrderId,
				TransactionType = LoyaltyTransactionType.Spend,
				PointsChanged = -info.Points,
				Reason = string.IsNullOrWhiteSpace(info.Reason)
					? info.VoucherId.HasValue
						? $"Redeemed for Voucher {info.VoucherId}"
						: info.OrderId.HasValue ? $"Redeemed for Order {info.OrderId} returned" : "Manual point redemption"
					: info.Reason.Trim()
			};
		}

		private static void ValidateCreateArgs(Guid userId, int points)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			if (points <= 0)
				throw DomainException.BadRequest("Points must be greater than 0.");
		}

		// Records
		public record EarnTransactionInfo
		{
			public required int Points { get; init; }
			public Guid? OrderId { get; init; }
			public string? Reason { get; init; }
		}

		public record SpendTransactionInfo
		{
			public required int Points { get; init; }
			public Guid? VoucherId { get; init; }
			public Guid? OrderId { get; init; }
			public string? Reason { get; init; }
		}

		public record ManualTransactionInfo
		{
			public required LoyaltyTransactionType TransactionType { get; init; }
			public required int Points { get; init; }
			public required string Reason { get; init; }
		}
	}
}
