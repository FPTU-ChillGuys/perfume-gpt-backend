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
		public static LoyaltyTransaction CreateManual(Guid userId, LoyaltyTransactionType transactionType, int points, string reason)
		{
			if (string.IsNullOrWhiteSpace(reason))
				throw DomainException.BadRequest("Reason is required for manual loyalty point changes.");

			return transactionType switch
			{
				LoyaltyTransactionType.Earn => CreateEarn(userId, points, reason: reason),
				LoyaltyTransactionType.Spend => CreateSpend(userId, points, reason: reason),
				_ => throw DomainException.BadRequest("Unsupported loyalty transaction type.")
			};
		}

		public static LoyaltyTransaction CreateEarn(Guid userId, int points, Guid? orderId = null, string? reason = null)
		{
			ValidateCreateArgs(userId, points);

			return new LoyaltyTransaction
			{
				UserId = userId,
				OrderId = orderId,
				TransactionType = LoyaltyTransactionType.Earn,
				PointsChanged = points,
				Reason = string.IsNullOrWhiteSpace(reason)
					? orderId.HasValue ? $"Earned from Order {orderId}" : "Manual point addition"
					: reason.Trim()
			};
		}

		public static LoyaltyTransaction CreateSpend(
			Guid userId,
			int points,
			Guid? voucherId = null,
			Guid? orderId = null,
			string? reason = null)
		{
			ValidateCreateArgs(userId, points);

			return new LoyaltyTransaction
			{
				UserId = userId,
				VoucherId = voucherId,
				OrderId = orderId,
				TransactionType = LoyaltyTransactionType.Spend,
				PointsChanged = -points,
				Reason = string.IsNullOrWhiteSpace(reason)
					? voucherId.HasValue
						? $"Redeemed for Voucher {voucherId}"
						: orderId.HasValue ? $"Redeemed for Order {orderId} returned" : "Manual point redemption"
					: reason.Trim()
			};
		}

		private static void ValidateCreateArgs(Guid userId, int points)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			if (points <= 0)
				throw DomainException.BadRequest("Points must be greater than 0.");
		}
	}
}
