using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Services;
using StackExchange.Redis;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties
{
	/// <summary>
	/// A background service that subscribes to Redis Pub/Sub channels to handle
	/// data requests from the AI backend. It follows a request-response pattern:
	/// the AI backend publishes a request with a reply channel, this service fetches
	/// the data and publishes the result back to that reply channel.
	/// </summary>
	public class RedisSubscriberService : BackgroundService
	{
		private const string CatalogRequestChannel = "catalog_request";

		private readonly IConnectionMultiplexer _redis;
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<RedisSubscriberService> _logger;

		public RedisSubscriberService(
			IConnectionMultiplexer redis,
			IServiceScopeFactory scopeFactory,
			ILogger<RedisSubscriberService> logger)
		{
			_redis = redis;
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			//while (!stoppingToken.IsCancellationRequested)
			//{
			//	try
			//	{
			//		_logger.LogInformation("[Redis] Attempting to subscribe to channel: {Channel}", CatalogRequestChannel);
			//		var subscriber = _redis.GetSubscriber();

			//		await subscriber.SubscribeAsync(
			//			RedisChannel.Literal(CatalogRequestChannel),
			//			async (channel, message) => await HandleCatalogRequestAsync(message));

			//		_logger.LogInformation("[Redis] Successfully subscribed to channel: {Channel}", CatalogRequestChannel);

			//		// Wait here while the subscription is active. 
			//		// SE.Redis will handle re-subscriptions automatically if the connection drops.
			//		// We only exit this delay if the stoppingToken is cancelled.
			//		await Task.Delay(Timeout.Infinite, stoppingToken);
			//	}
			//	catch (OperationCanceledException)
			//	{
			//		// App is shutting down, normal behavior
			//		break;
			//	}
			//	catch (Exception ex)
			//	{
			//		_logger.LogError(ex, "[Redis] Failed to subscribe to {Channel}. Retrying in 5 seconds...", CatalogRequestChannel);
			//		try
			//		{
			//			await Task.Delay(5000, stoppingToken);
			//		}
			//		catch (OperationCanceledException)
			//		{
			//			break;
			//		}
			//	}
			//}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			var subscriber = _redis.GetSubscriber();
			await subscriber.UnsubscribeAsync(RedisChannel.Literal(CatalogRequestChannel));
			_logger.LogInformation("[Redis] Unsubscribed from channel: {Channel}", CatalogRequestChannel);
			await base.StopAsync(cancellationToken);
		}

		private async Task HandleCatalogRequestAsync(RedisValue message)
		{
			if (message.IsNullOrEmpty)
				return;

			string? replyChannel = null;

			try
			{
				using var doc = JsonDocument.Parse(message.ToString());
				var root = doc.RootElement;

				// Parse variantId (required)
				if (!root.TryGetProperty("variantId", out var variantIdElement)
					|| !Guid.TryParse(variantIdElement.GetString(), out var variantId))
				{
					_logger.LogWarning("[Redis] catalog_request received but missing or invalid 'variantId'. Message: {Message}", message);
					return;
				}

				// Parse replyChannel (required for response routing)
				if (!root.TryGetProperty("replyChannel", out var replyChannelElement)
					|| string.IsNullOrWhiteSpace(replyChannelElement.GetString()))
				{
					_logger.LogWarning("[Redis] catalog_request received but missing 'replyChannel'. Message: {Message}", message);
					return;
				}

				replyChannel = replyChannelElement.GetString()!;

				_logger.LogInformation("[Redis] Handling catalog_request for variantId={VariantId}, replyChannel={ReplyChannel}", variantId, replyChannel);

				// Resolve scoped services (ISourcingCatalogService depends on IUnitOfWork which is Scoped)
				await using var scope = _scopeFactory.CreateAsyncScope();
				var catalogService = scope.ServiceProvider.GetRequiredService<ISourcingCatalogService>();

				var response = await catalogService.GetCatalogsAsync(supplierId: null, variantId: variantId);

				var payload = JsonSerializer.Serialize(new
				{
					variantId = variantId.ToString(),
					catalogs = response.Payload
				}, new JsonSerializerOptions
				{
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase
				});

				var publisher = _redis.GetSubscriber();
				await publisher.PublishAsync(RedisChannel.Literal(replyChannel), payload);

				_logger.LogInformation("[Redis] Published catalog response to {ReplyChannel} ({Count} items)", replyChannel, response.Payload?.Count());
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Redis] Error handling catalog_request. Message: {Message}", message);

				// Attempt to send an error response so AI backend doesn't hang
				if (!string.IsNullOrWhiteSpace(replyChannel))
				{
					try
					{
						var errorPayload = JsonSerializer.Serialize(new { error = "Internal error processing catalog request." });
						var publisher = _redis.GetSubscriber();
						await publisher.PublishAsync(RedisChannel.Literal(replyChannel), errorPayload);
					}
					catch
					{
						// Swallow — don't let error reporting crash anything
					}
				}
			}
		}
	}
}
