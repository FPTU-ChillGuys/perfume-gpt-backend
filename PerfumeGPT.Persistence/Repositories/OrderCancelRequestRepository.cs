using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class OrderCancelRequestRepository : GenericRepository<OrderCancelRequest>, IOrderCancelRequestRepository
	{
		public OrderCancelRequestRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}