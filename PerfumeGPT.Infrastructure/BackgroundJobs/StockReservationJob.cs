using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class StockReservationJob
	{
		private readonly IStockReservationService _stockReservationService;
		private readonly ILogger<StockReservationJob> _logger;

		public StockReservationJob(IStockReservationService stockReservationService, ILogger<StockReservationJob> logger)
		{
			_stockReservationService = stockReservationService;
			_logger = logger;
		}

		public async Task ProcessExpiredReservationsAsync()
		{
			try
			{
				var result = await _stockReservationService.ProcessExpiredReservationsAsync();

				if (result > 0)
					_logger.LogInformation("Processed {Result} expired reservations.", result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing expired reservations.");
				throw; // Re-throw for Hangfire retry
			}
		}
	}
}
