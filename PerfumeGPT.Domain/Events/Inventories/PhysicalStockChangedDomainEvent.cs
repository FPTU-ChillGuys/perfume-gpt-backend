using PerfumeGPT.Domain.Commons.Events;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Events.Inventories
{
	public record PhysicalStockChangedDomainEvent(
		Guid VariantId,
		Guid BatchId,
		int QuantityChange, // Số âm hoặc dương
		int BalanceAfter,   // Tồn kho sau khi đổi
		StockTransactionType Type,
		Guid ReferenceId,   // ID của Order, ImportTicket, hoặc AdjustmentTicket
		string? Description,
		Guid? ActorId       // Người thực hiện (nếu có)
	) : IDomainEvent; // Hoặc INotification
}
