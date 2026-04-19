using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class InventoryLedger : BaseEntity<Guid>, IHasCreatedAt
	{
		protected InventoryLedger() { }

		public Guid VariantId { get; private set; }
		public Guid BatchId { get; private set; }

		public int QuantityChange { get; private set; } // +100 (Nhập), -5 (Bán), -1 (Hư hỏng)
		public int BalanceAfter { get; private set; } // Tồn kho CỦA LÔ ĐÓ sau khi biến động (Rất quan trọng để đối soát)

		public StockTransactionType Type { get; private set; } // Enum: Import, Sales, Damage, Expired...

		public Guid ReferenceId { get; private set; } // ID của OrderDetail, ImportTicketDetail...
		public string? Description { get; private set; } // "Xuất bán cho đơn ORD-123"

		public Guid? ActorId { get; private set; } // ID của nhân viên thao tác (Thủ kho)

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		public static InventoryLedger CreateLog(
			Guid variantId,
			Guid batchId,
			int quantityChange,
			int balanceAfter,
			StockTransactionType type,
			Guid referenceId,
			string? description,
			Guid? actorId)
		{
			return new InventoryLedger
			{
				CreatedAt = DateTime.UtcNow,
				VariantId = variantId,
				BatchId = batchId,
				QuantityChange = quantityChange,
				BalanceAfter = balanceAfter,
				Type = type,
				ReferenceId = referenceId,
				Description = description,
				ActorId = actorId
			};
		}
	}
}