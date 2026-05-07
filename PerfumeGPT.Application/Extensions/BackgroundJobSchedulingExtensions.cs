using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.GHNs;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Application.Interfaces.ThirdParties.BackgroundJobs;
using PerfumeGPT.Domain.Enums;
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

		public static bool EnqueueEmail(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			string to,
			string subject,
			string htmlBody)
		{
			return TryEnqueue<IEmailService>(
				backgroundJobService,
				logger,
				x => x.SendEmailAsync(to, subject, htmlBody),
				"Unable to enqueue email sending for recipient {Recipient}.",
				to);
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

		public static bool ScheduleLoyaltyPointsGrant(this IBackgroundJobService backgroundJobService, ILogger logger, Guid orderId, DateTime deliveredAtUtc, int orderRewardPointsInDays)
		{
			var normalizedDeliveredAt = deliveredAtUtc.Kind == DateTimeKind.Unspecified
				? DateTime.SpecifyKind(deliveredAtUtc, DateTimeKind.Utc)
				: deliveredAtUtc.ToUniversalTime();

			var scheduleAt = normalizedDeliveredAt.AddDays(orderRewardPointsInDays);
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

		public static bool EnqueueOnlineOrderStaffNotification(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid orderId,
			string orderCode,
			decimal totalAmount)
		{
			return TryEnqueue<INotificationService>(
				backgroundJobService,
				logger,
				x => x.SendToRoleAsync(
					UserRole.staff,
					"Đơn hàng online mới",
					$"Có đơn hàng Online #{orderCode} cần đóng gói. Tổng tiền: {totalAmount:N0}.",
					NotificationType.Info,
					orderId,
					NotifiReferecneType.Order,
					null),
				"Unable to enqueue staff notification for online order {OrderId}.",
				orderId);
		}

		public static bool EnqueueOrderCreatedRedisEvent(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid orderId,
			Guid userId)
		{
			return TryEnqueue<IRedisPublisherService>(
				backgroundJobService,
				logger,
				x => x.PublishOrderCreatedAsync(orderId, userId),
				"Unable to enqueue order_created redis event for order {OrderId}.",
				orderId);
		}

		public static bool EnqueueReviewCreatedRedisEvent(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid variantId)
		{
			return TryEnqueue<IRedisPublisherService>(
				backgroundJobService,
				logger,
				x => x.PublishReviewCreatedAsync(variantId),
				"Unable to enqueue review_created redis event for variant {VariantId}.",
				variantId);
		}

		public static bool EnqueueProductUpdatedRedisEvent(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid productId,
			string action)
		{
			return TryEnqueue<IRedisPublisherService>(
				backgroundJobService,
				logger,
				x => x.PublishProductUpdatedAsync(productId, action),
				"Unable to enqueue product:updated redis event for product {ProductId}.",
				productId);
		}

		public static bool EnqueueOrderPreparingNotification(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid orderId,
			string orderCode,
			Guid customerId)
		{
			return TryEnqueue<ICustomerNotificationAppService>(
				backgroundJobService,
				logger,
				x => x.NotifyOrderPreparingAsync(orderId, orderCode, customerId),
				"Unable to enqueue preparing notification for order {OrderId}.",
				orderId);
		}

		public static bool EnqueueRoleNotification(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			UserRole role,
			string title,
			string message,
			NotificationType type = NotificationType.Info,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null)
		{
			return TryEnqueue<INotificationService>(
				backgroundJobService,
				logger,
				x => x.SendToRoleAsync(role, title, message, type, referenceId, referenceType, null),
				"Unable to enqueue role notification for role {Role}.",
				role);
		}

		public static bool EnqueueCustomerNotificationWithFcm(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid customerId,
			string title,
			string message,
			NotificationType type = NotificationType.Info,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null)
		{
			return TryEnqueue<ICustomerNotificationAppService>(
				backgroundJobService,
				logger,
				x => x.NotifyCustomerWithFcmAsync(customerId, title, message, type, referenceId, referenceType),
				"Unable to enqueue customer notification with FCM for customer {CustomerId}.",
				customerId);
		}

		public static bool EnqueueStaffNotificationWithFcm(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			Guid staffId,
			string title,
			string message,
			NotificationType type = NotificationType.Info,
			Guid? referenceId = null,
			NotifiReferecneType? referenceType = null)
		{
			return TryEnqueue<ICustomerNotificationAppService>(
				backgroundJobService,
				logger,
				x => x.NotifyCustomerWithFcmAsync(staffId, title, message, type, referenceId, referenceType),
				"Unable to enqueue staff notification with FCM for staff {StaffId}.",
				staffId);
		}

		public static bool EnqueueCancelShippingOrder(
			this IBackgroundJobService backgroundJobService,
			ILogger logger,
			string trackingNumber)
		{
			return TryEnqueue<IGHNService>(
				backgroundJobService,
				logger,
				x => x.CancelOrderAsync(new CancelOrderRequest
				{
					TrackingNumbers = new List<string> { trackingNumber }
				}),
				"Unable to enqueue GHN cancel order for tracking number {TrackingNumber}.",
				trackingNumber);
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
