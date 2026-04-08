namespace PerfumeGPT.Application.DTOs.Responses.PayOs
{
	public record PayOsReturnResponse
	{
		public Guid OrderId { get; init; }
		public Guid PaymentId { get; init; }
		public bool IsSuccess { get; init; }
	}
}
