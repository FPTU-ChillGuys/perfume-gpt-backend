using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class StorePolicyRepository : GenericRepository<StorePolicy>, IStorePolicyRepository
	{
		public StorePolicyRepository(PerfumeDbContext context) : base(context) { }

		public async Task<StorePolicy?> GetCurrentPolicyAsync()
		{
			return await _context.StorePolicies
				.FirstOrDefaultAsync();
		}
	}
}