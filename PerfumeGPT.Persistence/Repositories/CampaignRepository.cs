using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;
using PerfumeGPT.Application.DTOs.Responses.Campaigns;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class CampaignRepository : GenericRepository<Campaign>, ICampaignRepository
	{
		public CampaignRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<CampaignResponse>> GetHomeCampaignsAsync()
		{
			var now = DateTime.UtcNow;

			return await _context.Campaigns
				.AsNoTracking()
				.Where(x => !x.IsDeleted
					&& x.Status == CampaignStatus.Active
					&& x.StartDate <= now
					&& x.EndDate >= now)
				.OrderByDescending(x => x.StartDate)
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
		}

		public async Task<List<CampaignLookupItem>> GetActiveCampaignLookupListAsync()
		{
			var now = DateTime.UtcNow;

			return await _context.Campaigns
				.AsNoTracking()
				.Where(x => !x.IsDeleted
					&& x.Status == CampaignStatus.Active
					&& x.StartDate <= now
					&& x.EndDate >= now)
				.OrderBy(x => x.Name)
				.Select(x => new CampaignLookupItem
				{
					Id = x.Id,
					Name = x.Name
				})
				.ToListAsync();
		}

		public async Task<(List<CampaignResponse> Items, int TotalCount)> GetPagedCampaignsAsync(GetPagedCampaignsRequest request)
		{
			IQueryable<Campaign> query = _context.Campaigns
				.Where(x => !x.IsDeleted);
			Expression<Func<Campaign, bool>> filter = x => true;

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();
				filter = filter.AndAlso(x => x.Name.Contains(searchTerm));
			}

			if (request.Status.HasValue)
			{
				var status = request.Status.Value;
				filter = filter.AndAlso(x => x.Status == status);
			}

			if (request.Type.HasValue)
			{
				var type = request.Type.Value;
				filter = filter.AndAlso(x => x.Type == type);
			}

			query = query.Where(filter);

			var totalCount = await query.CountAsync();
			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(Campaign.Name),
				nameof(Campaign.StartDate),
				nameof(Campaign.EndDate),
				nameof(Campaign.Type),
				nameof(Campaign.Status),
				nameof(Campaign.CreatedAt)
			};
			var sortBy = request.SortBy?.Trim();
			sortBy = !string.IsNullOrWhiteSpace(sortBy)
				? (sortBy.Length == 1
					? char.ToUpper(sortBy[0]).ToString()
					: char.ToUpper(sortBy[0]) + sortBy.Substring(1))
				: null;
			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(x => x.CreatedAt);

			var items = await sortedQuery
				.AsNoTracking()
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
					 ProductVariantId = x.TargetProductVariantId,
					 Sku = x.ProductVariant.Sku,
					 PrimaryImageUrl = x.ProductVariant.Product.Media
						 .Where(m => m.IsPrimary && !m.IsDeleted)
						 .Select(i => i.Url)
						 .FirstOrDefault(),
					 ProductName = x.ProductVariant.Product.Name ?? string.Empty,
					 BatchId = x.BatchId,
					 BatchCode = x.Batch != null ? x.Batch.BatchCode : null,
					 ItemType = x.ItemType,
					 DiscountType = x.DiscountType,
					 DiscountValue = x.DiscountValue,
					 StartDate = x.Campaign.StartDate,
					 EndDate = x.Campaign.EndDate,
					 MaxUsage = x.MaxUsage,
					 CurrentUsage = x.CurrentUsage
				 })
				 .ToListAsync();
		}

		public async Task<CampaignPromotionItemResponse?> GetCampaignItemByIdAsync(Guid campaignId, Guid itemId, bool asNoTracking = true)
		{
			IQueryable<PromotionItem> query = _context.Promotions
				.Where(x => x.CampaignId == campaignId && x.Id == itemId && !x.IsDeleted);

			if (asNoTracking)
			{
				query = query.AsNoTracking();
			}

			return await query
				.Select(x => new CampaignPromotionItemResponse
				{
					Id = x.Id,
					CampaignId = x.CampaignId,
					ProductVariantId = x.TargetProductVariantId,
					Sku = x.ProductVariant.Sku,
					PrimaryImageUrl = x.ProductVariant.Product.Media
						 .Where(m => m.IsPrimary && !m.IsDeleted)
						 .Select(i => i.Url)
						 .FirstOrDefault(),
					ProductName = x.ProductVariant.Product.Name ?? string.Empty,
					BatchId = x.BatchId,
					BatchCode = x.Batch != null ? x.Batch.BatchCode : null,
					ItemType = x.ItemType,
					DiscountType = x.DiscountType,
					DiscountValue = x.DiscountValue,
					StartDate = x.Campaign.StartDate,
					EndDate = x.Campaign.EndDate,
					MaxUsage = x.MaxUsage,
					CurrentUsage = x.CurrentUsage
				})
				.FirstOrDefaultAsync();
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
