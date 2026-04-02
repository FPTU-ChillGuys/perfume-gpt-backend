namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record GetOrderInfoRequest
	{
		public required List<string> TrackingNumbers { get; init; }
	}
}
