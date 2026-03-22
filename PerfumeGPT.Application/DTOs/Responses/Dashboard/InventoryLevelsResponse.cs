namespace PerfumeGPT.Application.DTOs.Responses.Dashboard
{
	public class InventoryLevelsResponse
	{
		public int TotalVariants { get; set; }
		public int TotalStockQuantity { get; set; }
		public int TotalAvailableQuantity { get; set; }
		public int LowStockVariantsCount { get; set; }
		public int OutOfStockVariantsCount { get; set; }
		public int TotalBatches { get; set; }
		public int ExpiredBatchesCount { get; set; }
		public int ExpiringSoonCount { get; set; }
	}
}
