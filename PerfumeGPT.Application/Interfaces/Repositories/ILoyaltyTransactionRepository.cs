using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface ILoyaltyTransactionRepository : IGenericRepository<LoyaltyTransaction>
	{
		Task<int> GetPointBalanceAsync(Guid userId);
	}
}
