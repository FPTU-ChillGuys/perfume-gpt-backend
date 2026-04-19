using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class LoyaltyTransaction : BaseEntity<Guid>, IHasCreatedAt
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

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static LoyaltyTransaction CreateManual(Guid userId, ManualTransactionInfo info)
		{
			if (string.IsNullOrWhiteSpace(info.Reason))
				throw DomainException.BadRequest("Lí do giao dịch là bắt buộc cho các giao dịch thủ công.");

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
				_ => throw DomainException.BadRequest("Loại giao dịch điểm trung thành không được hỗ trợ.")
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
					? info.OrderId.HasValue ? $"Đã nhận từ Đơn hàng {info.OrderId}" : "Thêm điểm thủ công"
					: info.Reason.Trim(),
				CreatedAt = DateTime.UtcNow
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
						? $"Đã đổi cho Voucher {info.VoucherId}"
						: info.OrderId.HasValue ? $"Đã đổi cho Đơn hàng {info.OrderId} trả lại" : "Đổi điểm thủ công"
					: info.Reason.Trim(),
				CreatedAt = DateTime.UtcNow
			};
		}

		private static void ValidateCreateArgs(Guid userId, int points)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("ID người dùng là bắt buộc.");

			if (points <= 0)
				throw DomainException.BadRequest("Số điểm phải lớn hơn 0.");
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
