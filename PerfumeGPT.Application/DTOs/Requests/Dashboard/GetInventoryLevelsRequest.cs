namespace PerfumeGPT.Application.DTOs.Requests.Dashboard
{
	public class GetInventoryLevelsRequest
	{
		public int ExpiringWithinDays { get; set; } = 30;
	}
}
