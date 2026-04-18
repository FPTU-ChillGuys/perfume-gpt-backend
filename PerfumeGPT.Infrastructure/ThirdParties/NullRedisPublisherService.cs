using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	/// <summary>
	/// No-op fallback implementation used when Redis is unavailable at startup.
	/// Ensures the application continues to function normally without Redis.
	/// </summary>
	public class NullRedisPublisherService : IRedisPublisherService
	{
		private readonly ILogger<NullRedisPublisherService> _logger;

		public NullRedisPublisherService(ILogger<NullRedisPublisherService> logger)
		{
			_logger = logger;
		}

		public Task PublishOrderCreatedAsync(Guid orderId, Guid userId)
		{
			_logger.LogDebug("[Redis] NullPublisher: Skipping order_created publish (Redis unavailable) for orderId={OrderId}", orderId);
			return Task.CompletedTask;
		}

		public Task PublishReviewCreatedAsync(Guid variantId)
		{
			_logger.LogDebug("[Redis] NullPublisher: Skipping review_created publish (Redis unavailable) for variantId={VariantId}", variantId);
			return Task.CompletedTask;
		}
	}
}
