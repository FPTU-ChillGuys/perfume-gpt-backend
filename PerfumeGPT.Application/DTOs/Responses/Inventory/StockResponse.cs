namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public class StockResponse
	{
		public Guid Id { get; set; }
		public Guid VariantId { get; set; }
		public string VariantSku { get; set; } = null!;
		public string ProductName { get; set; } = null!;
		public int VolumeMl { get; set; }
		public string ConcentrationName { get; set; } = null!;
		public int TotalQuantity { get; set; }
		public int LowStockThreshold { get; set; }
		public bool IsLowStock { get; set; }
	}
}
