using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CampaignRepository : GenericRepository<Campaign>, ICampaignRepository
	{
		public CampaignRepository(PerfumeDbContext context) : base(context) { }

		public async Task<(List<CampaignResponse> Items, int TotalCount)> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			IQueryable<Campaign> query = _context.Campaigns
				.Where(x => !x.IsDeleted
					&& (string.IsNullOrWhiteSpace(request.SearchTerm) || x.Name.Contains(request.SearchTerm))
					&& (!request.Status.HasValue || x.Status == request.Status.Value)
					&& (!request.Type.HasValue || x.Type == request.Type.Value));

			var totalCount = await query.CountAsync();
			var items = await query
				.AsNoTracking()
				.ApplySorting(request.SortBy, request.IsDescending)
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
			  .Select(x => new CampaignResponse
			  {
				  Id = x.Id,
				  Name = x.Name,
				  Description = x.Description,
				  StartDate = x.StartDate,
				  EndDate = x.EndDate,
				  Type = x.Type,
				  Status = x.Status
			  })
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<CampaignResponse?> GetCampaignByIdDtoAsync(Guid campaignId)
		{
			return await _context.Campaigns
				.AsNoTracking()
				.Where(x => x.Id == campaignId && !x.IsDeleted)
			  .Select(x => new CampaignResponse
			  {
				  Id = x.Id,
				  Name = x.Name,
				  Description = x.Description,
				  StartDate = x.StartDate,
				  EndDate = x.EndDate,
				  Type = x.Type,
				  Status = x.Status
			  })
				.FirstOrDefaultAsync();
		}

		public async Task<List<CampaignPromotionItemResponse>> GetCampaignItemsAsync(Guid campaignId, bool asNoTracking = true)
		{
			IQueryable<PromotionItem> query = _context.Promotions
				.Where(x => x.CampaignId == campaignId && !x.IsDeleted)
				.OrderByDescending(x => x.CreatedAt);

			if (asNoTracking)
			{
				query = query.AsNoTracking();
			}

			return await query
				 .Select(x => new CampaignPromotionItemResponse
				 {
					 Id = x.Id,
					 CampaignId = x.CampaignId,
					 ProductVariantId = x.ProductVariantId,
					 BatchId = x.BatchId,
					 Name = x.ProductVariant.Product.Name ?? string.Empty,
					 ItemType = x.ItemType,
					 StartDate = x.Campaign.StartDate,
					 EndDate = x.Campaign.EndDate,
					 AutoStopWhenBatchEmpty = x.AutoStopWhenBatchEmpty,
					 MaxUsage = x.MaxUsage,
					 CurrentUsage = x.CurrentUsage
				 })
				 .ToListAsync();
		}

		public async Task<Campaign?> GetCampaignWithDetailsAsync(Guid campaignId)
		{
			return await _context.Campaigns
				.Include(x => x.Items.Where(i => !i.IsDeleted))
				.Include(x => x.Vouchers.Where(v => !v.IsDeleted))
				.AsSplitQuery()
				.FirstOrDefaultAsync(x => x.Id == campaignId && !x.IsDeleted);
		}
	}
}
