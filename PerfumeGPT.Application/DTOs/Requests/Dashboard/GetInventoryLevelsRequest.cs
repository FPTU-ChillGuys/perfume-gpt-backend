namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public record GetInventoryLevelsRequest
	{
		public int ExpiringWithinDays { get; init; } = 30;
	}
}
