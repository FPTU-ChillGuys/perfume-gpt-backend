using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PerfumeGPT.Infrastructure.BackgroundJobs.Schedulers
{
	public class StartupJobScheduler : IHostedService
	{
		private readonly IServiceScopeFactory _serviceScopeFactory;

		public StartupJobScheduler(IServiceScopeFactory serviceScopeFactory)
		{
			_serviceScopeFactory = serviceScopeFactory;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceScopeFactory.CreateScope();

			// Cleanup expired orders/reservations every minute
			RecurringJob.AddOrUpdate<StockReservationJob>(
			 "cleanup-expired-orders-and-reservations",
				job => job.CleanupExpiredOrdersAndReservationsAsync(),
				"* * * * *"); // Cron: every minute

			// Cleanup expired temporary media every hour
			RecurringJob.AddOrUpdate<TemporaryMediaCleanupJob>(
				"cleanup-expired-temporary-media",
				job => job.CleanupExpiredMediaAsync(),
				"0 * * * *"); // Cron: every hour at minute 0 (e.g., 1:00, 2:00, 3:00, etc.)

			// Sync GHN shipping statuses every 15 minutes
			RecurringJob.AddOrUpdate<ShippingStatusSyncJob>(
				"sync-ghn-shipping-status",
				job => job.SyncGhnShippingStatusAsync(),
				"*/15 * * * *"); // Cron: every 15 minutes

			// Send low stock alert email to admins every day at 08:00 UTC
			RecurringJob.AddOrUpdate<LowStockAlertJob>(
				"send-low-stock-alert-to-admins",
				job => job.SendLowStockAlertToAdminsAsync(),
				"0 8 * * *");

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
