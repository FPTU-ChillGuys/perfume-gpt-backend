using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Notifications
{
	public record NotificationListItemResponse
	{
		public Guid Id { get; init; }
		public Guid? UserId { get; init; }
		public string? TargetRole { get; init; }
		public string? Title { get; init; }
		public string? Message { get; init; }
		public NotificationType Type { get; init; }
		public Guid? ReferenceId { get; init; }
		public NotifiReferecneType? ReferenceType { get; init; }
		public string? MetadataJson { get; init; }
		public bool IsRead { get; init; }
		public DateTime CreatedAt { get; init; }
	}
}
