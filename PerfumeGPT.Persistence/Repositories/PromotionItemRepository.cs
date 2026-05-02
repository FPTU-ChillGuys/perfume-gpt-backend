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

			// Lưu ý: Đảm bảo DbSet của bạn tên là Promotions hoặc PromotionItems tùy cấu hình DbContext nhé.
			return await _context.Promotions
				.Where(p =>
					ids.Contains(p.CampaignId)
					&& p.IsActive
					&& !p.IsDeleted
					&& !p.Campaign.IsDeleted // Bổ sung check Campaign không bị xóa mềm
					&& p.Campaign.Status == CampaignStatus.Active
					&& p.Campaign.StartDate <= now
					&& p.Campaign.EndDate >= now
					&& (!p.MaxUsage.HasValue || p.CurrentUsage < p.MaxUsage.Value))
				.AsNoTracking()
				.ToListAsync();
		}

		public async Task<List<PromotionItem>> GetActiveClearancePromotionsByVariantIdAsync(Guid variantId, DateTime now)
		{
			if (variantId == Guid.Empty)
			{
				return [];
			}

			return await _context.Promotions
				.Where(i => i.TargetProductVariantId == variantId
					&& i.IsActive
					&& !i.IsDeleted // Bổ sung check Promotion không bị xóa mềm
					&& i.BatchId.HasValue
					&& !i.Campaign.IsDeleted // Bổ sung check Campaign không bị xóa mềm
					&& i.Campaign.Status == CampaignStatus.Active
					&& i.Campaign.StartDate <= now
					&& i.Campaign.EndDate >= now
					// Bổ sung check Quota
					&& (!i.MaxUsage.HasValue || i.CurrentUsage < i.MaxUsage.Value))
				.Include(i => i.Campaign)
				.AsNoTracking()
				.ToListAsync();
		}
	}
}
