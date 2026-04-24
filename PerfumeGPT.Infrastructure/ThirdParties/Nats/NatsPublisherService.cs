using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using NATS.Client.Core;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats
{
	/// <summary>
	/// Publishes application events to NATS channels for cross-service communication.
	/// All operations are wrapped in try/catch to ensure NATS failures never break the main flow.
	/// </summary>
	public class NatsPublisherService : INatsPublisherService
	{
		private const string OrderCreatedChannel = "order_created";
		private const string ReviewCreatedChannel = "review_created";

		private readonly INatsConnection _nats;
		private readonly ILogger<NatsPublisherService> _logger;

		public NatsPublisherService(INatsConnection nats, ILogger<NatsPublisherService> logger)
		{
			_nats = nats;
			_logger = logger;
		}

		public async Task PublishOrderCreatedAsync(Guid orderId, Guid userId)
		{
			try
			{
				var payload = JsonSerializer.Serialize(new
				{
					orderId = orderId.ToString(),
					userId = userId.ToString()
				});

				await _nats.PublishAsync(OrderCreatedChannel, payload);

				_logger.LogInformation("[NATS] Published order_created: orderId={OrderId}, userId={UserId}", orderId, userId);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[NATS] Failed to publish order_created for orderId={OrderId}. Skipping.", orderId);
			}
		}

		public async Task PublishReviewCreatedAsync(Guid variantId)
		{
			try
			{
				var payload = JsonSerializer.Serialize(new
				{
					variantId = variantId.ToString()
				});

				await _nats.PublishAsync(ReviewCreatedChannel, payload);

				_logger.LogInformation("[NATS] Published review_created: variantId={VariantId}", variantId);
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "[NATS] Failed to publish review_created for variantId={VariantId}. Skipping.", variantId);
			}
		}
	}
}
