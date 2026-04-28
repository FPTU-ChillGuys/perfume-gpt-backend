using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Pages;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Pages;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PageRepository : GenericRepository<SystemPage>, IPageRepository
	{
		public PageRepository(PerfumeDbContext context) : base(context) { }

		public async Task<SystemPage?> GetBySlugAsync(string slug, bool asNoTracking = false)
		{
			var query = _context.SystemPages
				.Include(x => x.PageImages)
				.AsQueryable();
			if (asNoTracking)
			{
				query = query.AsNoTracking();
			}

			return await query.FirstOrDefaultAsync(x => x.Slug == slug);
		}

		public async Task<SystemPage?> GetPublishedBySlugAsync(string slug)
		{
			return await _context.SystemPages
				.Include(x => x.PageImages)
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Slug == slug && x.IsPublished);
		}

		public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null)
		{
			return excludeId.HasValue
				? await _context.SystemPages.AnyAsync(x => x.Slug == slug && x.Id != excludeId.Value)
				: await _context.SystemPages.AnyAsync(x => x.Slug == slug);
		}

		public async Task<(List<PageResponse> Items, int TotalCount)> GetPagedPagesAsync(GetPagedPageRequest request)
		{
			Expression<Func<SystemPage, bool>> filter = x => true;

			if (!string.IsNullOrWhiteSpace(request.SearchTerm))
			{
				var searchTerm = request.SearchTerm.Trim();

				var titleFilter = EfCollationExtensions.CollateContains<SystemPage>(
					x => x.Title,
					searchTerm);

				var slugFilter = EfCollationExtensions.CollateContains<SystemPage>(
					x => x.Slug,
					searchTerm);

				var searchFilter = titleFilter.OrElse(slugFilter);
				filter = filter.AndAlso(searchFilter);
			}

			IQueryable<SystemPage> query = _context.SystemPages.AsNoTracking().Where(filter);

			var totalCount = await query.CountAsync();

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(SystemPage.Slug),
				nameof(SystemPage.Title),
				nameof(SystemPage.IsPublished),
				nameof(SystemPage.UpdatedAt),
				nameof(SystemPage.CreatedAt)
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
				: query.OrderByDescending(x => x.CreatedAt);

			var items = await sortedQuery
				.Skip((request.PageNumber - 1) * request.PageSize)
				.Take(request.PageSize)
				.Select(x => new PageResponse
				{
					Slug = x.Slug,
					Title = x.Title,
					HtmlContent = x.HtmlContent,
					IsPublished = x.IsPublished,
					MetaDescription = x.MetaDescription,
					Images = x.PageImages
						.Where(m => !m.IsDeleted)
						.Select(m => new MediaResponse
						{
							Id = m.Id,
							Url = m.Url,
							AltText = m.AltText,
							DisplayOrder = m.DisplayOrder,
							IsPrimary = m.IsPrimary,
							FileSize = m.FileSize,
							MimeType = m.MimeType
						})
						.ToList(),
					UpdatedAt = x.UpdatedAt ?? x.CreatedAt
				})
				.ToListAsync();

			return (items, totalCount);
		}
	}
}
