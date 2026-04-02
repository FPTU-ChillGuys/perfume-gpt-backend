using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Notification : BaseEntity<Guid>, IHasCreatedAt
	{
		protected Notification() { }

		public Guid UserId { get; private set; }
		public string? Message { get; private set; }
		public NotificationType Type { get; private set; }
		public Guid? StockId { get; private set; }
		public Guid? OrderId { get; private set; }
		public Guid? VoucherId { get; private set; }
		public Guid? BatchId { get; private set; }
		public bool IsRead { get; private set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual Stock? Stock { get; set; }
		public virtual Order? Order { get; set; }
		public virtual Voucher? Voucher { get; set; }
		public virtual Batch? Batch { get; set; }

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Notification Create(Guid userId, NotificationPayload payload)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			return new Notification
			{
				UserId = userId,
				Type = payload.Type,
				Message = string.IsNullOrWhiteSpace(payload.Message) ? null : payload.Message.Trim(),
				StockId = payload.StockId,
				OrderId = payload.OrderId,
				VoucherId = payload.VoucherId,
				BatchId = payload.BatchId,
				IsRead = false
			};
		}

		// Business logic methods
		public void MarkAsRead()
		{
			IsRead = true;
		}

		// Records
		public record NotificationPayload
		{
			public required NotificationType Type { get; init; }
			public string? Message { get; init; }

			public Guid? StockId { get; init; }
			public Guid? OrderId { get; init; }
			public Guid? VoucherId { get; init; }
			public Guid? BatchId { get; init; }
		}
	}
}
