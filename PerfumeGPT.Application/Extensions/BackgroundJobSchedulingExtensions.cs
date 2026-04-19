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
			var normalizedEndDate = NormalizeToUtc(endDate);

			return TrySchedule<ICampaignEndAppService>(
				   backgroundJobService,
				   logger,
				   x => x.MarkCampaignAsEndedAsync(campaignId),
				normalizedEndDate,
				   "Unable to schedule campaign end job for campaign {CampaignId}.",
				   campaignId);
		}

		public static bool ScheduleCampaignStart(this IBackgroundJobService backgroundJobService, ILogger logger, Guid campaignId, DateTime startDate)
		{
			var normalizedStartDate = NormalizeToUtc(startDate);

			return TrySchedule<ICampaignStartAppService>(
				   backgroundJobService,
				   logger,
				   x => x.MarkCampaignAsStartedAsync(campaignId),
				  normalizedStartDate,
				   "Unable to schedule campaign start job for campaign {CampaignId}.",
				   campaignId);
		}

		public static bool ScheduleBannerStart(this IBackgroundJobService backgroundJobService, ILogger logger, Guid bannerId, DateTime startDate)
		{
			return TrySchedule<IBannerStartAppService>(
				backgroundJobService,
				logger,
				x => x.MarkBannerAsStartedAsync(bannerId),
				startDate,
				"Unable to schedule banner start job for banner {BannerId}.",
				bannerId);
		}

		public static bool ScheduleBannerEnd(this IBackgroundJobService backgroundJobService, ILogger logger, Guid bannerId, DateTime endDate)
		{
			return TrySchedule<IBannerEndAppService>(
				backgroundJobService,
				logger,
				x => x.MarkBannerAsEndedAsync(bannerId),
				endDate,
				"Unable to schedule banner end job for banner {BannerId}.",
				bannerId);
		}

		public static bool ScheduleLoyaltyPointsGrant(this IBackgroundJobService backgroundJobService, ILogger logger, Guid orderId, DateTime deliveredAtUtc)
		{
			var normalizedDeliveredAt = deliveredAtUtc.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(deliveredAtUtc, DateTimeKind.Utc)
				: deliveredAtUtc.ToUniversalTime();

			var scheduleAt = normalizedDeliveredAt.AddDays(10);
			if (scheduleAt < DateTime.UtcNow)
			{
				scheduleAt = DateTime.UtcNow;
			}

			return TrySchedule<ILoyaltyPointsAppService>(
				backgroundJobService,
				logger,
				x => x.GrantPointsIfEligibleAsync(orderId),
				scheduleAt,
				"Unable to schedule loyalty point grant job for order {OrderId}.",
				orderId);
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

		private static DateTime NormalizeToUtc(DateTime dateTime)
		{
			if (dateTime.Kind == DateTimeKind.Utc)
			{
				return dateTime;
			}

			if (dateTime.Kind == DateTimeKind.Local)
			{
				return dateTime.ToUniversalTime();
			}

			return DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
		}
	}
}
