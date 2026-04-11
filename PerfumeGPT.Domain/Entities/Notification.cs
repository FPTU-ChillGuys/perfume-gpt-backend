using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Notification : BaseEntity<Guid>, IHasCreatedAt
	{
		protected Notification() { }

		public Guid? UserId { get; private set; }
		public string? TargetRole { get; private set; }
		public string Title { get; private set; } = null!;
		public string Message { get; private set; } = null!;
		public NotificationType Type { get; private set; }
		public Guid? ReferenceId { get; private set; }
		public NotifiReferecneType? ReferenceType { get; private set; }
		public string? MetadataJson { get; private set; }
		public bool IsRead { get; private set; }

		// Navigation properties
		public virtual User? User { get; set; }

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Notification CreateForUser(Guid userId, NotificationPayload payload)
		{
			if (userId == Guid.Empty)
				throw DomainException.BadRequest("User ID is required.");
			if (string.IsNullOrWhiteSpace(payload.Title))
				throw DomainException.BadRequest("Title is required.");
			if (string.IsNullOrWhiteSpace(payload.Message))
				throw DomainException.BadRequest("Message is required.");

			return new Notification
			{
				UserId = userId,
				TargetRole = null,
				Title = payload.Title.Trim(),
				Type = payload.Type,
				Message = payload.Message.Trim(),
				ReferenceId = payload.ReferenceId,
				ReferenceType = payload.ReferenceType,
				MetadataJson = string.IsNullOrWhiteSpace(payload.MetadataJson) ? null : payload.MetadataJson,
				IsRead = false
			};
		}

		public static Notification CreateForRole(string targetRole, NotificationPayload payload)
		{
			if (string.IsNullOrWhiteSpace(targetRole))
				throw DomainException.BadRequest("Target role is required.");
			if (string.IsNullOrWhiteSpace(payload.Title))
				throw DomainException.BadRequest("Title is required.");
			if (string.IsNullOrWhiteSpace(payload.Message))
				throw DomainException.BadRequest("Message is required.");

			return new Notification
			{
				UserId = null,
				TargetRole = targetRole.Trim(),
				Title = payload.Title.Trim(),
				Type = payload.Type,
				Message = payload.Message.Trim(),
				ReferenceId = payload.ReferenceId,
				ReferenceType = payload.ReferenceType,
				MetadataJson = string.IsNullOrWhiteSpace(payload.MetadataJson) ? null : payload.MetadataJson,
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
			public required string Title { get; init; }
			public required string Message { get; init; }
			public Guid? ReferenceId { get; init; }
			public NotifiReferecneType? ReferenceType { get; init; }
			public string? MetadataJson { get; init; }
		}
	}
}
