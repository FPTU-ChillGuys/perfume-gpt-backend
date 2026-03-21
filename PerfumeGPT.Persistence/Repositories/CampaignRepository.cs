using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CampaignRepository : GenericRepository<Campaign>, ICampaignRepository
	{
		public CampaignRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
