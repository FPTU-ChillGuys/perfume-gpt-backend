using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PromotionItemRepository : GenericRepository<PromotionItem>, IPromotionItemRepository
	{
		public PromotionItemRepository(PerfumeDbContext context) : base(context)
		{
		}
	}
}
