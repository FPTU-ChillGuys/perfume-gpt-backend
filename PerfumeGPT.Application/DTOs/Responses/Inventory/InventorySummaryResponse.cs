namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public record InventorySummaryResponse
	{
		public int TotalVariants { get; init; }
		public int TotalStockQuantity { get; init; }
		public int LowStockVariantsCount { get; init; }
		public int OutOfStockVariantsCount { get; init; }
		public int TotalBatches { get; init; }
		public int ExpiredBatchesCount { get; init; }
		public int ExpiringSoonCount { get; init; } // Expiring within 30 days
	}
}
