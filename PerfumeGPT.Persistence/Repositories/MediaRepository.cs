using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class MediaRepository : GenericRepository<Media>, IMediaRepository
	{
		public MediaRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<Media>> GetMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			return await _context.Media
				.Where(m => m.EntityType == entityType
					&& (entityType == EntityType.Product
						? m.ProductId == entityId
						: m.ProductVariantId == entityId)
					&& !m.IsDeleted)
				.OrderBy(m => m.DisplayOrder)
				.ToListAsync();
		}

		public async Task<Media?> GetPrimaryMediaAsync(EntityType entityType, Guid entityId)
		{
			return await _context.Media
				.WherePrimaryForEntity(entityType, entityId)
				.FirstOrDefaultAsync();
		}

		public async Task<int> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaItems = await _context.Media
			.Where(m => m.EntityType == entityType
				&& (entityType == EntityType.Product
					? m.ProductId == entityId
					: m.ProductVariantId == entityId)
				&& !m.IsDeleted)
			.ToListAsync();

			foreach (var media in mediaItems)
			{
				_context.Media.Remove(media);
			}

			return mediaItems.Count;
		}
	}
}
