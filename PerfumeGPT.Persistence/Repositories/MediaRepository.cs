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
		public MediaRepository(PerfumeDbContext context) : base(context) { }

		public async Task<List<Media>> GetMediaByEntityTypeAsync(EntityType entityType, Guid entityId)
		=> await _context.Media
			.WhereEntityNotDeleted(entityType, entityId)
			.OrderBy(m => m.DisplayOrder)
			.ToListAsync();

		public async Task<Media?> GetPrimaryMediaAsync(EntityType entityType, Guid entityId)
		=> await _context.Media
			.WherePrimaryForEntity(entityType, entityId)
			.FirstOrDefaultAsync();

		public async Task<int> DeleteAllMediaByEntityAsync(EntityType entityType, Guid entityId)
		{
			var mediaItems = await _context.Media
				.WhereEntityNotDeleted(entityType, entityId)
				.ToListAsync();

			foreach (var media in mediaItems)
			{
				_context.Media.Remove(media);
			}

			return mediaItems.Count;
		}
	}
}
