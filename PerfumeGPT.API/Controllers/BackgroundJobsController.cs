using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Infrastructure.BackgroundJobs;

namespace PerfumeGPT.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class BackgroundJobsController : ControllerBase
	{
		private readonly IStockReservationService _stockReservationService;

		public BackgroundJobsController(IStockReservationService stockReservationService)
		{
			_stockReservationService = stockReservationService;
		}

		/// <summary>
		/// Manually trigger expired reservation processing (for testing)
		/// </summary>
		[HttpPost("process-expired-reservations")]
		[Authorize(Roles = "Admin,Manager")]
		public async Task<IActionResult> ProcessExpiredReservations()
		{
			var result = await _stockReservationService.ProcessExpiredReservationsAsync();
			
			if (result.Success)
			{
				return Ok(BaseResponse<int>.Ok(
					result.Payload,
					$"Processed {result.Payload} expired reservations successfully."));
			}

			return BadRequest(BaseResponse<int>.Fail(
				result.Message ?? "Failed to process expired reservations.",
				result.ErrorType));
		}

		/// <summary>
		/// Trigger the background job immediately (for testing)
		/// </summary>
		[HttpPost("trigger-job")]
		[Authorize(Roles = "Admin")]
		public IActionResult TriggerJob()
		{
			BackgroundJob.Enqueue<StockReservationJob>(job => job.ProcessExpiredReservationsAsync());
			return Ok(BaseResponse<string>.Ok("Background job triggered successfully."));
		}

		/// <summary>
		/// Get Hangfire dashboard URL
		/// </summary>
		[HttpGet("dashboard-url")]
		public IActionResult GetDashboardUrl()
		{
			var baseUrl = $"{Request.Scheme}://{Request.Host}";
			return Ok(BaseResponse<string>.Ok($"{baseUrl}/hangfire"));
		}
	}
}
