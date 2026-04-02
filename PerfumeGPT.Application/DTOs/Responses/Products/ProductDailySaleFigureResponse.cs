namespace PerfumeGPT.Application.DTOs.Responses.Products
{
	public record ProductDailySaleFigureResponse
	{
		public Guid ProductId { get; init; }
		public required string ProductName { get; init; }
		public required List<VariantDailySaleFigure> DailySaleFigures { get; init; }
	}

	public record VariantDailySaleFigure
	{
		public Guid VariantId { get; init; }
		public required string VariantName { get; init; }
		public DateOnly Date { get; init; }
		public int QuantitySold { get; init; }
	}
}
