using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;

namespace PerfumeGPT.Persistence.Repositories
{
	public class LoyaltyTransactionRepository : GenericRepository<LoyaltyTransaction>, ILoyaltyTransactionRepository
	{
		public LoyaltyTransactionRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<int> GetPointBalanceAsync(Guid userId)
		{
			return await _context.LoyaltyTransactions.Where(lt => lt.UserId == userId).SumAsync(lt => lt.PointsChanged);
		}
	}
}
