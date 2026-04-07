namespace PerfumeGPT.Application.DTOs.Requests.Momos
{
	public record MomoQueryRequest
	{
		public Guid PaymentId { get; init; }
	}
}
