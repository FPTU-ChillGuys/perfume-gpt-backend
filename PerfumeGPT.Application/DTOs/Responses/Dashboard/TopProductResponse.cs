namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public class TopProductResponse
	{
		public Guid ProductId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		public int TotalUnitsSold { get; set; }
		public decimal Revenue { get; set; }
	}
}
