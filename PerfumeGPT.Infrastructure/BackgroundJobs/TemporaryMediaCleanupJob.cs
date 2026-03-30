using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class TemporaryMediaCleanupJob
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISupabaseService _supabaseService;
		private readonly ILogger<TemporaryMediaCleanupJob> _logger;

		public TemporaryMediaCleanupJob(
			IUnitOfWork unitOfWork,
			ISupabaseService supabaseService,
			ILogger<TemporaryMediaCleanupJob> logger)
		{
			_unitOfWork = unitOfWork;
			_supabaseService = supabaseService;
			_logger = logger;
		}

		public async Task CleanupExpiredMediaAsync()
		{
			_logger.LogInformation("Starting temporary media cleanup at {Time}", DateTime.UtcNow);

			var expiredMedia = await _unitOfWork.TemporaryMedia.GetExpiredMediaAsync();
			if (!expiredMedia.Any())
			{
				_logger.LogInformation("No expired media found");
				return;
			}

			_logger.LogInformation("Found {Count} expired media items to process", expiredMedia.Count);

			var successfulDbDeletes = 0;
			var successfulCloudDeletes = 0;

			foreach (var media in expiredMedia)
			{
				try
				{
					if (!string.IsNullOrEmpty(media.PublicId))
					{
						await _supabaseService.DeleteImageAsync(media.PublicId, "TemporaryReviews");
						successfulCloudDeletes++;
					}

					_unitOfWork.TemporaryMedia.Remove(media);
					successfulDbDeletes++;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to delete media {MediaId} (PublicId: {PublicId})", media.Id, media.PublicId);
				}
			}

			if (successfulDbDeletes > 0)
			{
				await _unitOfWork.SaveChangesAsync();
			}

			_logger.LogInformation("Cleanup completed. Deleted {DbCount} DB records and {CloudCount} files from Supabase.", successfulDbDeletes, successfulCloudDeletes);
		}
	}
}
