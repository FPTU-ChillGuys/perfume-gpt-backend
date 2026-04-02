namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public record InventoryLevelsResponse
	{
		public int TotalVariants { get; init; }
		public int TotalStockQuantity { get; init; }
		public int TotalAvailableQuantity { get; init; }
		public int LowStockVariantsCount { get; init; }
		public int OutOfStockVariantsCount { get; init; }
		public int TotalBatches { get; init; }
		public int ExpiredBatchesCount { get; init; }
		public int ExpiringSoonCount { get; init; }
	}
}
