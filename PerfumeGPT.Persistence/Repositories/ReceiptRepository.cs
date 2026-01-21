using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class ReceiptRepository : GenericRepository<Receipt>, IReceiptRepository
	{
		public ReceiptRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<Receipt?> GetByTransactionIdAsync(Guid transactionId)
		{
			return await _context.Receipts.FirstOrDefaultAsync(r => r.TransactionId == transactionId);
		}
	}
}
