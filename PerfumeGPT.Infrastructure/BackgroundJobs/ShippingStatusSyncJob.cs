using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class ShippingStatusSyncJob
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IShippingService _shippingService;
		private readonly ILogger<ShippingStatusSyncJob> _logger;

		public ShippingStatusSyncJob(
			IUnitOfWork unitOfWork,
			ILogger<ShippingStatusSyncJob> logger,
			IShippingService shippingService)
		{
			_unitOfWork = unitOfWork;
			_logger = logger;
			_shippingService = shippingService;
		}

		public async Task SyncGhnShippingStatusAsync()
		{
			_logger.LogInformation("Starting GHN shipping status sync...");

			var candidates = await _unitOfWork.ShippingInfos.GetSyncCandidatesForGhnAsync();
			if (candidates.Count == 0) return;

			var totalUpdatedCount = 0;
			var chunks = candidates.Chunk(50);

			foreach (var chunk in chunks)
			{
				var chunkUpdatedCount = 0;
				foreach (var shippingInfo in chunk)
				{
					var isUpdated = await _shippingService.SyncSingleShippingInfoAsync(shippingInfo);

					if (isUpdated)
					{
						chunkUpdatedCount++;
						totalUpdatedCount++;
					}

					await Task.Delay(200);
				}

				if (chunkUpdatedCount > 0)
				{
					await _unitOfWork.SaveChangesAsync();
				}
			}

			_logger.LogInformation("GHN status sync completed. Updated {Count} records.", totalUpdatedCount);
		}
	}
}
