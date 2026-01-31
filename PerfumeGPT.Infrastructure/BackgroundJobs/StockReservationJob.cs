using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.BackgroundJobs
{
	/// <summary>
	/// Background job to process expired stock reservations
	/// </summary>
	public class StockReservationJob
	{
		private readonly IStockReservationService _stockReservationService;

		public StockReservationJob(IStockReservationService stockReservationService)
		{
			_stockReservationService = stockReservationService;
		}

		/// <summary>
		/// Process expired reservations - called by Hangfire scheduler
		/// </summary>
		public async Task ProcessExpiredReservationsAsync()
		{
			try
			{
				var result = await _stockReservationService.ProcessExpiredReservationsAsync();
				
				if (result.Success)
				{
					Console.WriteLine($"[StockReservationJob] Processed {result.Payload} expired reservations at {DateTime.UtcNow}");
				}
				else
				{
					Console.WriteLine($"[StockReservationJob] Failed to process expired reservations: {result.Message}");
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
