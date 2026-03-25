using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	public class StockReservationJob
	{
		private readonly IStockReservationService _stockReservationService;

		public StockReservationJob(IStockReservationService stockReservationService)
		{
			_stockReservationService = stockReservationService;
		}

		public async Task ProcessExpiredReservationsAsync()
		{
			try
			{
				var result = await _stockReservationService.ProcessExpiredReservationsAsync();

				if (result > 0)
				{
					Console.WriteLine($"[StockReservationJob] Processed {result} expired reservations at {DateTime.UtcNow}");
				}
				else
				{
					Console.WriteLine($"[StockReservationJob] Failed to process expired reservations: {result}");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[StockReservationJob] Error processing expired reservations: {ex.Message}");
				throw; // Re-throw so Hangfire can retry
			}
		}
	}
}
