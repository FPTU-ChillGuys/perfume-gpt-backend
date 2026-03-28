using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IOrderReturnRequestRepository : IGenericRepository<OrderReturnRequest>
	{
		Task<OrderReturnRequest?> GetByIdWithOrderAsync(Guid requestId);
		Task<OrderReturnRequest?> GetByIdWithOrderDetailsAsync(Guid requestId);
	}
}
