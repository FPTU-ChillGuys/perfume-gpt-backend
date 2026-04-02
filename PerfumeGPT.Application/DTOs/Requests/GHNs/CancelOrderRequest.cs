namespace PerfumeGPT.Application.DTOs.Requests.GHNs
{
	public record CancelOrderRequest
	{
		public required List<string> TrackingNumbers { get; init; }
	}
}
