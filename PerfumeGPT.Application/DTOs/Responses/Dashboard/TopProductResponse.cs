namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public record TopProductResponse
	{
		public Guid ProductId { get; init; }
		public required string ProductName { get; init; }
		public int TotalUnitsSold { get; init; }
		public decimal Revenue { get; init; }
	}
}
