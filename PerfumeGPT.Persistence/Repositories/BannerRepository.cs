using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class BannerRepository : GenericRepository<Banner>, IBannerRepository
	{
		public BannerRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<BannerResponse>> GetVisibleBannersAsync(BannerPosition? position = null)
		{
			var now = DateTime.UtcNow;

			return await _context.Banners
				.AsNoTracking()
				.Where(x => x.IsActive
					&& (!x.StartDate.HasValue || x.StartDate <= now)
					&& (!x.EndDate.HasValue || x.EndDate >= now)
					&& (!position.HasValue || x.Position == position.Value))
				.OrderBy(x => x.DisplayOrder)
				.ThenByDescending(x => x.CreatedAt)
				.Select(x => new BannerResponse
				{
					Id = x.Id,
					Title = x.Title,
					ImageUrl = x.ImageUrl,
					ImagePublicId = x.ImagePublicId,
					MobileImageUrl = x.MobileImageUrl,
					MobileImagePublicId = x.MobileImagePublicId,
					AltText = x.AltText,
					Position = x.Position,
					DisplayOrder = x.DisplayOrder,
					IsActive = x.IsActive,
					StartDate = x.StartDate,
					EndDate = x.EndDate,
					LinkType = x.LinkType,
					LinkTarget = x.LinkTarget,
					CreatedAt = x.CreatedAt,
					UpdatedAt = x.UpdatedAt
				})
				.ToListAsync();
		}

		public async Task<(List<BannerResponse> Items, int TotalCount)> GetPagedBannersAsync(GetPagedBannersRequest request)
		{
         Expression<Func<Banner, bool>> filter = x => true;

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();
				Expression<Func<Banner, bool>> searchFilter = x => x.Title.Contains(searchTerm);
				filter = filter.AndAlso(searchFilter);
			}

			if (request.Position.HasValue)
			{
				var position = request.Position.Value;
				Expression<Func<Banner, bool>> positionFilter = x => x.Position == position;
				filter = filter.AndAlso(positionFilter);
			}

			if (request.IsActive.HasValue)
			{
				var isActive = request.IsActive.Value;
				Expression<Func<Banner, bool>> activeFilter = x => x.IsActive == isActive;
				filter = filter.AndAlso(activeFilter);
			}

			IQueryable<Banner> query = _context.Banners.Where(filter);

			var totalCount = await query.CountAsync();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(Banner.Title),
				nameof(Banner.DisplayOrder),
				nameof(Banner.Position),
				nameof(Banner.CreatedAt),
				nameof(Banner.UpdatedAt)
			};

			string? sortBy = null;
			if (!string.IsNullOrWhiteSpace(request.SortBy))
			{
				var trimmedSortBy = request.SortBy.Trim();
				sortBy = trimmedSortBy.Length == 1
					? char.ToUpper(trimmedSortBy[0]).ToString()
					: char.ToUpper(trimmedSortBy[0]) + trimmedSortBy.Substring(1);
			}

			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAt);

			var items = await sortedQuery
				.AsNoTracking()
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new BannerResponse
				{
					Id = x.Id,
					Title = x.Title,
					ImageUrl = x.ImageUrl,
					ImagePublicId = x.ImagePublicId,
					MobileImageUrl = x.MobileImageUrl,
					MobileImagePublicId = x.MobileImagePublicId,
					AltText = x.AltText,
					Position = x.Position,
					DisplayOrder = x.DisplayOrder,
					IsActive = x.IsActive,
					StartDate = x.StartDate,
					EndDate = x.EndDate,
					LinkType = x.LinkType,
					LinkTarget = x.LinkTarget,
					CreatedAt = x.CreatedAt,
					UpdatedAt = x.UpdatedAt
				})
				.ToListAsync();

			return (items, totalCount);
		}

		public async Task<BannerResponse?> GetBannerByIdDtoAsync(Guid bannerId)
		{
			return await _context.Banners
				.AsNoTracking()
				.Where(x => x.Id == bannerId)
				.Select(x => new BannerResponse
				{
					Id = x.Id,
					Title = x.Title,
					ImageUrl = x.ImageUrl,
					ImagePublicId = x.ImagePublicId,
					MobileImageUrl = x.MobileImageUrl,
					MobileImagePublicId = x.MobileImagePublicId,
					AltText = x.AltText,
					Position = x.Position,
					DisplayOrder = x.DisplayOrder,
					IsActive = x.IsActive,
					StartDate = x.StartDate,
					EndDate = x.EndDate,
					LinkType = x.LinkType,
					LinkTarget = x.LinkTarget,
					CreatedAt = x.CreatedAt,
					UpdatedAt = x.UpdatedAt
				})
				.FirstOrDefaultAsync();
		}
	}
}
