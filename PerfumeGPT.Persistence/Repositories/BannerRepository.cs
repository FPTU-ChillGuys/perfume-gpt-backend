using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

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
			IQueryable<Banner> query = _context.Banners
				.Where(x => (string.IsNullOrWhiteSpace(request.SearchTerm) || x.Title.Contains(request.SearchTerm))
					&& (!request.Position.HasValue || x.Position == request.Position.Value)
					&& (!request.IsActive.HasValue || x.IsActive == request.IsActive.Value));

			var totalCount = await query.CountAsync();

			var sortedQuery = (request.SortBy?.ToLowerInvariant(), request.IsDescending) switch
			{
				("title", true) => query.OrderByDescending(x => x.Title),
				("title", false) => query.OrderBy(x => x.Title),
				("displayorder", true) => query.OrderByDescending(x => x.DisplayOrder),
				("displayorder", false) => query.OrderBy(x => x.DisplayOrder),
				("position", true) => query.OrderByDescending(x => x.Position),
				("position", false) => query.OrderBy(x => x.Position),
				("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
				("createdat", false) => query.OrderBy(x => x.CreatedAt),
				("updatedat", true) => query.OrderByDescending(x => x.UpdatedAt),
				("updatedat", false) => query.OrderBy(x => x.UpdatedAt),
				_ => query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAt)
			};

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
