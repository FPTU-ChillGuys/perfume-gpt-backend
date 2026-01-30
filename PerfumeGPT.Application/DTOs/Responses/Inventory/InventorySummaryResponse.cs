namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public class InventorySummaryResponse
	{
		public int TotalVariants { get; set; }
		public int TotalStockQuantity { get; set; }
		public int LowStockVariantsCount { get; set; }
		public int TotalBatches { get; set; }
		public int ExpiredBatchesCount { get; set; }
		public int ExpiringSoonCount { get; set; } // Expiring within 30 days
	}
}
