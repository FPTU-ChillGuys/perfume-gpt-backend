using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
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

		// Process expired stock reservations every 5 minutes
		RecurringJob.AddOrUpdate<StockReservationJob>(
			"process-expired-reservations",
			job => job.ProcessExpiredReservationsAsync(),
			"*/5 * * * *"); // Cron: every 5 minutes

		// Cleanup expired temporary media every hour
		RecurringJob.AddOrUpdate<TemporaryMediaCleanupJob>(
			"cleanup-expired-temporary-media",
			job => job.CleanupExpiredMediaAsync(),
			"0 * * * *"); // Cron: every hour at minute 0 (e.g., 1:00, 2:00, 3:00, etc.)

		return Task.CompletedTask;
	}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
