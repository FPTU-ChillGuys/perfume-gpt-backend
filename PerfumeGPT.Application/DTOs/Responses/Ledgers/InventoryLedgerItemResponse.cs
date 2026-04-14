using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.Ledgers
{
	public record InventoryLedgerItemResponse
	{
		public Guid Id { get; init; }
		public DateTime CreatedAt { get; init; }
		public Guid VariantId { get; init; }
		public Guid BatchId { get; init; }
		public int QuantityChange { get; init; }
		public int BalanceAfter { get; init; }
		public StockTransactionType Type { get; init; }
		public Guid ReferenceId { get; init; }
		public string? Description { get; init; }
		public Guid? ActorId { get; init; }
	}
}
