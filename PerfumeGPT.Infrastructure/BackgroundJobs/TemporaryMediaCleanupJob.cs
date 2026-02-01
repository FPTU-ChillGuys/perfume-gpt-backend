using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	/// <summary>
	/// Background job to cleanup expired temporary media
	/// </summary>
	public class TemporaryMediaCleanupJob
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly ISupabaseService _supabaseService;

		public TemporaryMediaCleanupJob(IUnitOfWork unitOfWork, ISupabaseService supabaseService)
		{
			_unitOfWork = unitOfWork;
			_supabaseService = supabaseService;
		}

		/// <summary>
		/// Cleanup expired temporary media - called by Hangfire scheduler
		/// </summary>
		public async Task CleanupExpiredMediaAsync()
		{
			try
			{
				Console.WriteLine($"[TemporaryMediaCleanupJob] Starting cleanup at {DateTime.UtcNow}");

				// Get all expired temporary media
				var expiredMedia = await _unitOfWork.TemporaryMedia.GetExpiredMediaAsync();

				if (!expiredMedia.Any())
				{
					Console.WriteLine("[TemporaryMediaCleanupJob] No expired media found");
					return;
				}

				Console.WriteLine($"[TemporaryMediaCleanupJob] Found {expiredMedia.Count} expired media items");

				var deletedCount = 0;
				var storageDeletedCount = 0;

				foreach (var media in expiredMedia)
				{
					try
					{
						// Delete from cloud storage (Supabase)
						if (!string.IsNullOrEmpty(media.PublicId))
						{
							await _supabaseService.DeleteImageAsync(media.PublicId, "TemporaryReviews");
							storageDeletedCount++;
						}
					}
					catch (Exception ex)
					{
						// Log but continue - we still want to delete the DB record
						Console.WriteLine($"[TemporaryMediaCleanupJob] Failed to delete {media.PublicId} from storage: {ex.Message}");
					}
				}

				// Delete all expired records from database
				deletedCount = await _unitOfWork.TemporaryMedia.DeleteExpiredMediaAsync();

				Console.WriteLine($"[TemporaryMediaCleanupJob] Successfully deleted {deletedCount} database records and {storageDeletedCount} files from storage at {DateTime.UtcNow}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[TemporaryMediaCleanupJob] Error during cleanup: {ex.Message}");
				Console.WriteLine($"[TemporaryMediaCleanupJob] Stack trace: {ex.StackTrace}");
				throw; // Re-throw so Hangfire can retry
			}
		}
	}
}
