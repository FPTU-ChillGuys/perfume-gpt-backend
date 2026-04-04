using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using System.Linq.Expressions;

namespace PerfumeGPT.Application.Extensions
{
	public static class BackgroundJobSchedulingExtensions
	{
		public static bool EnqueueInvoiceEmail(this IBackgroundJobService backgroundJobService, ILogger logger, Guid orderId)
		{
			return TryEnqueue<IInvoiceAppService>(
				  backgroundJobService,
				  logger,
				  x => x.SendInvoiceEmailIfNeededAsync(orderId),
				  "Unable to enqueue invoice email for order {OrderId}.",
				  orderId);
		}

		public static bool ScheduleCampaignEnd(this IBackgroundJobService backgroundJobService, ILogger logger, Guid campaignId, DateTime endDate)
		{
			return TrySchedule<ICampaignEndAppService>(
				   backgroundJobService,
				   logger,
				   x => x.MarkCampaignAsEndedAsync(campaignId),
				   endDate,
				   "Unable to schedule campaign end job for campaign {CampaignId}.",
				   campaignId);
		}

		private static bool TryEnqueue<TJob>(
			IBackgroundJobService backgroundJobService,
			ILogger logger,
			Expression<Func<TJob, Task>> methodCall,
			string warningMessage,
			params object[] args)
		{
			try
			{
				backgroundJobService.Enqueue(methodCall);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, warningMessage, args);
				return false;
			}
		}

		private static bool TrySchedule<TJob>(
			IBackgroundJobService backgroundJobService,
			ILogger logger,
			Expression<Func<TJob, Task>> methodCall,
			DateTime scheduledAt,
			string warningMessage,
			params object[] args)
		{
			try
			{
				backgroundJobService.Schedule(methodCall, scheduledAt);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, warningMessage, args);
				return false;
			}
		}
	}
}
