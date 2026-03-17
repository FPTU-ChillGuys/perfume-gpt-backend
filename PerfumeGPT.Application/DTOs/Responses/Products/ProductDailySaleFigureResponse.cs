namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public class ProductDailySaleFigureResponse
	{
		public Guid ProductId { get; set; }
		public string ProductName { get; set; } = string.Empty;
		public List<VariantDailySaleFigure> DailySaleFigures { get; set; } = [];
	}

	public class VariantDailySaleFigure
	{
		public Guid VariantId { get; set; }
		public string VariantName { get; set; } = string.Empty;
		public DateOnly Date { get; set; }
		public int QuantitySold { get; set; }
	}
}
