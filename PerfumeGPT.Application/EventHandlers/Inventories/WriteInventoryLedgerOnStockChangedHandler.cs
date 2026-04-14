using MediatR;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Events.Inventories;

namespace PerfumeGPT.Application.EventHandlers.Inventories
{
	public class WriteInventoryLedgerOnStockChangedHandler : INotificationHandler<PhysicalStockChangedDomainEvent>
	{
		private readonly IGenericRepository<InventoryLedger> _ledgerRepository;

		public WriteInventoryLedgerOnStockChangedHandler(IGenericRepository<InventoryLedger> ledgerRepository)
		{
			_ledgerRepository = ledgerRepository;
		}

		public async Task Handle(PhysicalStockChangedDomainEvent notification, CancellationToken cancellationToken)
		{
			// Khởi tạo trực tiếp từ Event
			var log = InventoryLedger.CreateLog(
				variantId: notification.VariantId,
				batchId: notification.BatchId,
				quantityChange: notification.QuantityChange,
				balanceAfter: notification.BalanceAfter,
				type: notification.Type,
				referenceId: notification.ReferenceId,
				description: notification.Description,
				actorId: notification.ActorId
			);

			// Ghi xuống DB (Chờ SaveChangesAsync chung của luồng chính dọn dẹp)
			await _ledgerRepository.AddAsync(log);
		}
	}
}
