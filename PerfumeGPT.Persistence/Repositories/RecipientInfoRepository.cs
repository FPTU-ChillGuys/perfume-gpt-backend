using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class RecipientInfoRepository : GenericRepository<RecipientInfo>, IRecipientInfoRepository
	{
		public RecipientInfoRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
