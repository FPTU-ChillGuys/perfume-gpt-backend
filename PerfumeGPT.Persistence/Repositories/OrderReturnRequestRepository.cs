using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderReturnRequestRepository : GenericRepository<OrderReturnRequest>, IOrderReturnRequestRepository
	{
		public OrderReturnRequestRepository(PerfumeDbContext context) : base(context) { }

		public async Task<OrderReturnRequest?> GetByIdWithOrderAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.Include(r => r.Order)
				.Include(r => r.ProofImages)
				.Include(r => r.ReturnDetails)
					.ThenInclude(d => d.OrderDetail)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == requestId);

		public async Task<OrderReturnRequest?> GetByIdWithOrderDetailsAsync(Guid requestId)
			=> await _context.OrderReturnRequests
				.Include(r => r.Order)
					.ThenInclude(o => o.OrderDetails)
				.Include(r => r.ProofImages)
				.Include(r => r.ReturnDetails)
					.ThenInclude(d => d.OrderDetail)
				.AsSplitQuery()
				.FirstOrDefaultAsync(r => r.Id == requestId);
	}
}
