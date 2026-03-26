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
		public static Notification Create(
			Guid userId,
			NotificationType type,
			string? message,
			Guid? stockId = null,
			Guid? orderId = null,
			Guid? voucherId = null,
			Guid? batchId = null)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			return new Notification
			{
				UserId = userId,
				Type = type,
				Message = string.IsNullOrWhiteSpace(message) ? null : message.Trim(),
				StockId = stockId,
				OrderId = orderId,
				VoucherId = voucherId,
				BatchId = batchId,
				IsRead = false
			};
		}

		// Business logic methods
		public void MarkAsRead()
		{
			IsRead = true;
		}
	}
}
