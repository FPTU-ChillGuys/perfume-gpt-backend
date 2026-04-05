namespace PerfumeGPT.Application.DTOs.Requests.OrderReturnRequests
{
	public record UpdateReturnRequestDto
	{
		public string? CustomerNote { get; init; }
		public List<Guid>? TemporaryMediaIds { get; init; }
		public List<Guid>? RemoveMediaIds { get; init; }
	}
}
