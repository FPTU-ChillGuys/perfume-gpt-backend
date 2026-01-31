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

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}
}
