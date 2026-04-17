using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PromotionItemRepository : GenericRepository<PromotionItem>, IPromotionItemRepository
	{
		public PromotionItemRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<PromotionItem>> GetActiveByCampaignIdsAsync(IEnumerable<Guid> campaignIds)
		{
			var now = DateTime.UtcNow;
			var ids = campaignIds
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			if (ids.Count == 0)
			{
				return [];
			}

			return await _context.Promotions
				.Where(p =>
					ids.Contains(p.CampaignId)
					&& p.IsActive
					&& !p.IsDeleted
					&& p.Campaign.Status == CampaignStatus.Active
					&& p.Campaign.StartDate <= now
					&& p.Campaign.EndDate >= now
					&& (!p.MaxUsage.HasValue || p.CurrentUsage < p.MaxUsage.Value))
				.AsNoTracking()
				.ToListAsync();
		}
	}
}
