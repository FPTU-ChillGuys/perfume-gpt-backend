namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
	public record VnPayReturnResponse
	{
		public Guid OrderId { get; init; }
		public Guid PaymentId { get; init; }
		public bool IsSuccess { get; init; }
	}
}
