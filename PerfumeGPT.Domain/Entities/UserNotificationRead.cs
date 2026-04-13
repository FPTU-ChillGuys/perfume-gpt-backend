using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class UserNotificationRead
	{
		protected UserNotificationRead() { }
		public Guid UserId { get; private set; }
		public Guid NotificationId { get; private set; }
		public DateTime ReadAt { get; private set; }

		// Navigation properties
		public virtual User User { get; set; } = null!;
		public virtual Notification Notification { get; set; } = null!;

		// Factory method
		public static UserNotificationRead Create(Guid userId, Guid notificationId)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");

			if (notificationId == Guid.Empty)
				throw DomainException.BadRequest("Notification ID is required.");

			return new UserNotificationRead
			{
				UserId = userId,
				NotificationId = notificationId,
				ReadAt = DateTime.UtcNow
			};
		}
	}
}
