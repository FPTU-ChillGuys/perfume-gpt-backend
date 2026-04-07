namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public record LowStockAlertItem
	{
		public Guid VariantId { get; init; }
		public required string VariantSku { get; init; }
		public required string ProductName { get; init; }
		public int TotalQuantity { get; init; }
		public int AvailableQuantity { get; init; }
		public int LowStockThreshold { get; init; }
	}
}
