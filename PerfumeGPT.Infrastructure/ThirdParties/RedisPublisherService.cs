using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using StackExchange.Redis;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	/// <summary>
	/// Publishes order events to Redis Pub/Sub channels for cross-service communication.
	/// All operations are wrapped in try/catch to ensure Redis failures never break the order flow.
	/// </summary>
	public class RedisPublisherService : IRedisPublisherService
	{
		private const string OrderCreatedChannel = "order_created";
		private const string ReviewCreatedChannel = "review_created";

		private readonly IConnectionMultiplexer _redis;
		private readonly ILogger<RedisPublisherService> _logger;

		public RedisPublisherService(IConnectionMultiplexer redis, ILogger<RedisPublisherService> logger)
		{
			_redis = redis;
			_logger = logger;
		}

		public async Task PublishOrderCreatedAsync(Guid orderId, Guid userId)
		{
			try
			{
				var publisher = _redis.GetSubscriber();
				var payload = JsonSerializer.Serialize(new
				{
					orderId = orderId.ToString(),
					userId = userId.ToString()
				});

				await publisher.PublishAsync(
					RedisChannel.Literal(OrderCreatedChannel),
					payload);

				_logger.LogInformation("[Redis] Published order_created: orderId={OrderId}, userId={UserId}", orderId, userId);
			}
			catch (Exception ex)
			{
				// Redis failure must NEVER break the order flow — log and continue
				_logger.LogWarning(ex, "[Redis] Failed to publish order_created for orderId={OrderId}. Skipping.", orderId);
			}
		}

		public async Task PublishReviewCreatedAsync(Guid variantId)
		{
			try
			{
				var publisher = _redis.GetSubscriber();
				var payload = JsonSerializer.Serialize(new
				{
					variantId = variantId.ToString()
				});

				await publisher.PublishAsync(
					RedisChannel.Literal(ReviewCreatedChannel),
					payload);

				_logger.LogInformation("[Redis] Published review_created: variantId={VariantId}", variantId);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[Redis] Failed to publish review_created for variantId={VariantId}. Skipping.", variantId);
			}
		}
	}
}
