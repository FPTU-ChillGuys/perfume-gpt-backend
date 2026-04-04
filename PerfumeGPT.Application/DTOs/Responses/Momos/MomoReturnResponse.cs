namespace PerfumeGPT.Application.DTOs.Responses.Momos
{
	public record MomoReturnResponse
	{
		public Guid OrderId { get; init; }
		public Guid PaymentId { get; init; }
		public bool IsSuccess { get; init; }
	}
}
