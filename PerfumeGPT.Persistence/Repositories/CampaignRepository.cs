using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
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

		public async Task<(IEnumerable<Campaign> Items, int TotalCount)> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			IQueryable<Campaign> query = _context.Set<Campaign>()
				.Where(x => !x.IsDeleted
					&& (string.IsNullOrWhiteSpace(request.SearchTerm) || x.Name.Contains(request.SearchTerm))
					&& (!request.Status.HasValue || x.Status == request.Status.Value)
					&& (!request.Type.HasValue || x.Type == request.Type.Value));

			query = request.SortBy?.ToLower() switch
			{
				"name" => request.IsDescending
					? query.OrderByDescending(x => x.Name)
					: query.OrderBy(x => x.Name),
				"startdate" => request.IsDescending
					? query.OrderByDescending(x => x.StartDate)
					: query.OrderBy(x => x.StartDate),
				"enddate" => request.IsDescending
					? query.OrderByDescending(x => x.EndDate)
					: query.OrderBy(x => x.EndDate),
				_ => request.IsDescending
					? query.OrderByDescending(x => x.CreatedAt)
					: query.OrderBy(x => x.CreatedAt)
			};

			var totalCount = await query.CountAsync();
			var items = await query
				.AsNoTracking()
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<IEnumerable<PromotionItem>> GetCampaignItemsAsync(Guid campaignId, bool asNoTracking = true)
		{
			IQueryable<PromotionItem> query = _context.Set<PromotionItem>()
				.Where(x => x.CampaignId == campaignId && !x.IsDeleted)
				.OrderByDescending(x => x.CreatedAt);

			if (asNoTracking)
			{
				query = query.AsNoTracking();
			}

			return await query.ToListAsync();
		}
	}
}
