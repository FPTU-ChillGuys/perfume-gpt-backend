namespace PerfumeGPT.Application.DTOs.Requests.Notifications
{
	public record SendPushNotificationRequest
	{
		public required string DeviceToken { get; init; }
		public required string Title { get; init; }
		public required string Body { get; init; }
		public Dictionary<string, string>? Data { get; init; }
	}
}
