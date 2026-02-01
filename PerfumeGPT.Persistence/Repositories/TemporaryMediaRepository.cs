using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class TemporaryMediaRepository : GenericRepository<TemporaryMedia>, ITemporaryMediaRepository
	{
		public TemporaryMediaRepository(PerfumeDbContext context) : base(context)
		{
		}

		public async Task<List<TemporaryMedia>> GetExpiredMediaAsync()
		{
			var now = DateTime.UtcNow;
			return await _context.TemporaryMedia
				.Where(tm => tm.ExpiresAt < now)
				.ToListAsync();
		}

		public async Task<List<TemporaryMedia>> GetByUserIdAsync(Guid userId)
		{
			return await _context.TemporaryMedia
				.Where(tm => tm.UploadedByUserId == userId)
				.OrderByDescending(tm => tm.CreatedAt)
				.ToListAsync();
		}

		public async Task<int> DeleteExpiredMediaAsync()
		{
			var now = DateTime.UtcNow;
			var expiredMedia = await _context.TemporaryMedia
				.Where(tm => tm.ExpiresAt < now)
				.ToListAsync();

			if (!expiredMedia.Any())
			{
				return 0;
			}

			_context.TemporaryMedia.RemoveRange(expiredMedia);
			return await _context.SaveChangesAsync();
		}
	}
}
