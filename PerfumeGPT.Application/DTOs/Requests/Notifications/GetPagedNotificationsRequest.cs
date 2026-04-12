using PerfumeGPT.Application.DTOs.Requests.Base;

namespace PerfumeGPT.Application.DTOs.Requests.Notifications
{
	public record GetPagedNotificationsRequest : PagingAndSortingQuery
	{
		public Guid? UserId { get; init; }
		public string? TargetRole { get; init; }
		public bool? IsRead { get; init; }
	}
}
