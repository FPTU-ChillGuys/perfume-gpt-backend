		using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.Interfaces.Services;
using StackExchange.Redis;
using System.Text.Json;
using PerfumeGPT.Application.DTOs.Responses.Analytics;
using PerfumeGPT.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;

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
		private const string InventoryRequestChannel = "inventory_data_request";
		private const string ReviewRequestChannel = "review_data_request";
		private const string SalesRequestChannel = "sales_data_request";

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
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					_logger.LogInformation("[Redis] Attempting to subscribe to channel: {Channel}", CatalogRequestChannel);
					var subscriber = _redis.GetSubscriber();

					await subscriber.SubscribeAsync(
						RedisChannel.Literal(CatalogRequestChannel),
						async (channel, message) => await HandleCatalogRequestAsync(message));

					await subscriber.SubscribeAsync(
						RedisChannel.Literal(InventoryRequestChannel),
						async (channel, message) => await HandleInventoryRequestAsync(message));

					await subscriber.SubscribeAsync(
						RedisChannel.Literal(SalesRequestChannel),
						async (channel, message) => await HandleSalesRequestAsync(message));

					_logger.LogInformation("[Redis] Successfully subscribed to channels: {Catalog}, {Inventory}, {Review}, {Sales}", 
						CatalogRequestChannel, InventoryRequestChannel, ReviewRequestChannel, SalesRequestChannel);

					// Wait here while the subscription is active. 
					// SE.Redis will handle re-subscriptions automatically if the connection drops.
					// We only exit this delay if the stoppingToken is cancelled.
					await Task.Delay(Timeout.Infinite, stoppingToken);
				}
				catch (OperationCanceledException)
				{
					// App is shutting down, normal behavior
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "[Redis] Failed to subscribe to {Channel}. Retrying in 5 seconds...", CatalogRequestChannel);
					try
					{
						await Task.Delay(5000, stoppingToken);
					}
					catch (OperationCanceledException)
					{
						break;
					}
				}
			}
		}

		public override async Task StopAsync(CancellationToken cancellationToken)
		{
			var subscriber = _redis.GetSubscriber();
			await subscriber.UnsubscribeAsync(RedisChannel.Literal(CatalogRequestChannel));
			await subscriber.UnsubscribeAsync(RedisChannel.Literal(InventoryRequestChannel));
			await subscriber.UnsubscribeAsync(RedisChannel.Literal(ReviewRequestChannel));
			await subscriber.UnsubscribeAsync(RedisChannel.Literal(SalesRequestChannel));
			_logger.LogInformation("[Redis] Unsubscribed from all request channels.");
			await base.StopAsync(cancellationToken);
		}

		private async Task ExecuteBaseHandlerAsync(
			RedisValue message,
			string channelName,
			Func<IServiceScope, string, JsonElement, Task<object?>> processor)
		{
			if (message.IsNullOrEmpty) return;
			string? replyChannel = null;

			try
			{
				using var doc = JsonDocument.Parse(message.ToString());
				var root = doc.RootElement;

				if (!root.TryGetProperty("replyChannel", out var replyChannelElement))
				{
					_logger.LogWarning("[Redis] {Channel} received but missing 'replyChannel'.", channelName);
					return;
				}
				replyChannel = replyChannelElement.GetString();
				if (string.IsNullOrEmpty(replyChannel)) return;

				// Protocol refactor: Extract action and payload separately
				string action = root.TryGetProperty("action", out var actionElement) ? actionElement.GetString() ?? "" : "";
				JsonElement payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : root;

				_logger.LogDebug("[Redis] Processing {Channel}:{Action} for replyChannel={ReplyChannel}", channelName, action, replyChannel);

				await using var scope = _scopeFactory.CreateAsyncScope();
				var resultPayload = await processor(scope, action, payload);

				var options = new JsonSerializerOptions 
				{ 
					PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
					WriteIndented = false 
				};
				var responseJson = JsonSerializer.Serialize(resultPayload, options);

				var publisher = _redis.GetSubscriber();
				await publisher.PublishAsync(RedisChannel.Literal(replyChannel), responseJson);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "[Redis] Error handling {Channel}.", channelName);
				if (!string.IsNullOrEmpty(replyChannel))
				{
					try
					{
						var errorPayload = JsonSerializer.Serialize(new { error = $"Internal error: {ex.Message}" }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
						await _redis.GetSubscriber().PublishAsync(RedisChannel.Literal(replyChannel), errorPayload);
					}
					catch { /* Ignore fails during error reporting */ }
				}
			}
		}

		private async Task HandleCatalogRequestAsync(RedisValue message)
		{
			await ExecuteBaseHandlerAsync(message, CatalogRequestChannel, async (scope, action, payload) =>
			{
				if (!payload.TryGetProperty("variantId", out var variantIdElement)
					|| !Guid.TryParse(variantIdElement.GetString(), out var variantId))
				{
					throw new ArgumentException("Missing or invalid 'variantId'");
				}

				var catalogService = scope.ServiceProvider.GetRequiredService<ISourcingCatalogService>();
				var response = await catalogService.GetCatalogsAsync(supplierId: null, variantId: variantId);

				return new
				{
					variantId = variantId.ToString(),
					catalogs = response.Payload
				};
			});
		}

		private async Task HandleInventoryRequestAsync(RedisValue message)
		{
			await ExecuteBaseHandlerAsync(message, InventoryRequestChannel, async (scope, action, payload) =>
			{
				var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
				var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

				var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

				return action switch
				{
					"getOverallStats" => (await stockService.GetInventorySummaryAsync()).Payload,
					"getInventory" => await GetInventoryAsObjectAsync(stockService, payload, options),
					"getBatches" => (await batchService.GetBatchesAsync(JsonSerializer.Deserialize<PerfumeGPT.Application.DTOs.Requests.Inventory.Batches.GetBatchesRequest>(payload.GetRawText(), options)!)).Payload,
					_ => throw new ArgumentException($"Invalid action: {action}")
				};
			});
		}

		private async Task<object> GetInventoryAsObjectAsync(IStockService stockService, JsonElement payload, JsonSerializerOptions options)
		{
			var request = JsonSerializer.Deserialize<PerfumeGPT.Application.DTOs.Requests.Inventory.GetPagedInventoryRequest>(payload.GetRawText(), options)!;
			var result = await stockService.GetInventoryAsync(request);
			if (result.Payload == null) return new { totalCount = 0, items = new object[0] };
			
			// Cast Items to object so System.Text.Json serializes their runtime type (AiStockResponse)
			return new {
				TotalCount = result.Payload.TotalCount,
				Items = result.Payload.Items.Cast<object>()
			};
		}

		private async Task HandleReviewRequestAsync(RedisValue message)
		{
			await ExecuteBaseHandlerAsync(message, ReviewRequestChannel, async (scope, action, payload) =>
			{
				var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
				var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

				return action switch
				{
					"getStats" => payload.TryGetProperty("variantId", out var vIdEl) && Guid.TryParse(vIdEl.GetString(), out var vId)
						? (await reviewService.GetVariantReviewStatisticsAsync(vId)).Payload
						: throw new ArgumentException("Missing or invalid variantId for stats"),

					"getList" => (await reviewService.GetReviewsAsync(JsonSerializer.Deserialize<PerfumeGPT.Application.DTOs.Requests.Reviews.GetPagedReviewsRequest>(payload.GetRawText(), options)!)).Payload,

					"getVariantReviews" => payload.TryGetProperty("variantId", out var vIdEl2) && Guid.TryParse(vIdEl2.GetString(), out var vId2)
						? (await reviewService.GetVariantReviewsAsync(vId2)).Payload
						: throw new ArgumentException("Missing or invalid variantId for reviews"),

					_ => throw new ArgumentException($"Invalid action: {action}")
				};
			});
		}
		private async Task HandleSalesRequestAsync(RedisValue message)
		{
			await ExecuteBaseHandlerAsync(message, SalesRequestChannel, async (scope, action, payload) =>
			{
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
				return action switch
				{
					"getSalesAnalytics" => await HandleGetSalesAnalyticsAsync(unitOfWork, payload),
					_ => throw new ArgumentException($"Invalid action: {action}")
				};
			});
		}

		private async Task<object> HandleGetSalesAnalyticsAsync(IUnitOfWork unitOfWork, JsonElement payload)
		{
			int months = payload.TryGetProperty("months", out var m) ? m.GetInt32() : 2;
			var startDate = DateTime.UtcNow.AddMonths(-months);

			// 1. Get all active variants with basic info
			var stocks = await unitOfWork.Stocks.GetAllAsync(
				s => !s.ProductVariant.IsDeleted && !s.ProductVariant.Product.IsDeleted,
				include: q => q.Include(s => s.ProductVariant).ThenInclude(pv => pv.Product)
							   .Include(s => s.ProductVariant).ThenInclude(pv => pv.Concentration),
				asNoTracking: true
			);

			// 2. Get all order details from the last N months
			var orders = await unitOfWork.Orders.GetAllAsync(
				o => o.CreatedAt >= startDate && 
					 o.Status != OrderStatus.Cancelled && 
					 o.Status != OrderStatus.Returned,
				include: q => q.Include(o => o.OrderDetails),
				asNoTracking: true
			);

			// Group order details by variant and date
			var dailySalesByVariant = orders
				.SelectMany(o => o.OrderDetails.Select(od => new { od.VariantId, Date = o.CreatedAt.ToString("yyyy-MM-dd"), od.Quantity, od.UnitPrice }))
				.GroupBy(x => new { x.VariantId, x.Date })
				.Select(g => new { g.Key.VariantId, g.Key.Date, QuantitySold = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.Quantity * x.UnitPrice) })
				.GroupBy(x => x.VariantId)
				.ToDictionary(g => g.Key, g => g.ToList());

			var results = stocks.Select(s => new VariantSalesAnalyticsResponse
			{
				VariantId = s.VariantId,
				Sku = s.ProductVariant.Sku,
				ProductName = s.ProductVariant.Product.Name,
				VolumeMl = s.ProductVariant.VolumeMl,
				Type = s.ProductVariant.Type.ToString(),
				BasePrice = s.ProductVariant.BasePrice,
				Status = s.ProductVariant.Status.ToString(),
				ConcentrationName = s.ProductVariant.Concentration?.Name,
				DailySales = dailySalesByVariant.TryGetValue(s.VariantId, out var sales) 
					? sales.Select(ds => new DailySalesData { Date = ds.Date, QuantitySold = ds.QuantitySold, Revenue = (decimal)ds.Revenue }).ToList()
					: new List<DailySalesData>()
			}).ToList();

			return results;
		}
	}
}
