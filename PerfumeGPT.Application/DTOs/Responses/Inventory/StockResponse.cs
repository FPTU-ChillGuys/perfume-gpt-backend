using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Inventory
{
	public record StockResponse
	{
		public Guid Id { get; init; }
		public Guid VariantId { get; init; }
		public required string VariantSku { get; init; }
		public required string ProductName { get; init; }
		public required string VariantImageUrl { get; init; }
		public int VolumeMl { get; init; }
		public required string ConcentrationName { get; init; }
		public int TotalQuantity { get; init; }
		public int AvailableQuantity { get; init; }
		public int LowStockThreshold { get; init; }
		public StockStatus Status { get; init; }
	}
}
