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

		public async Task CleanupExpiredOrdersAndReservationsAsync()
		{
			try
			{
				var (ordersCleaned, reservationsCleaned) = await _stockReservationService.CleanupExpiredOrdersAndReservationsAsync();

				if (ordersCleaned > 0 || reservationsCleaned > 0)
					_logger.LogInformation("Processed {OrdersCleaned} expired orders and {ReservationsCleaned} expired reservations.", ordersCleaned, reservationsCleaned);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error cleaning up expired orders/reservations.");
				throw; // Re-throw for Hangfire retry
			}
		}
	}
}
