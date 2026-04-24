using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats
{
    public class NatsSubscriberService : BackgroundService
    {
        private const string InventoryRequestChannel = "inventory_data_request";
        private const string ReviewRequestChannel = "review_data_request";
        private const string SalesRequestChannel = "sales_data_request";
        private const string ProductRequestChannel = "product_data_request";
        private const string CartRequestChannel = "cart_data_request";
        private const string OrderRequestChannel = "order_data_request";

        private readonly INatsConnection _nats;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NatsSubscriberService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public NatsSubscriberService(
            INatsConnection nats,
            IServiceScopeFactory scopeFactory,
            ILogger<NatsSubscriberService> logger)
        {
            _nats = nats;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channels = new[] 
            { 
                ProductRequestChannel, CartRequestChannel, OrderRequestChannel,
                InventoryRequestChannel, ReviewRequestChannel, SalesRequestChannel
            };

            foreach (var channel in channels)
            {
                // Subscribe in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await foreach (var msg in _nats.SubscribeAsync<string>(channel, cancellationToken: stoppingToken))
                        {
                            _ = HandleMessageAsync(channel, msg);
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[NATS] Subscription error on {Channel}", channel);
                    }
                }, stoppingToken);
                
                _logger.LogInformation("[NATS] Subscribed to channel: {Channel}", channel);
            }

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task HandleMessageAsync(string channelName, NatsMsg<string> msg)
        {
            if (string.IsNullOrEmpty(msg.Data)) return;

            try
            {
                using var doc = JsonDocument.Parse(msg.Data);
                var root = doc.RootElement;

                string action = root.TryGetProperty("action", out var actionElement) ? actionElement.GetString() ?? "" : "";
                JsonElement payload = root.TryGetProperty("payload", out var payloadElement) ? payloadElement : root;

                _logger.LogDebug("[NATS] Request {Channel}:{Action}", channelName, action);

                await using var scope = _scopeFactory.CreateAsyncScope();
                object? resultPayload = channelName switch
                {
                    ProductRequestChannel => await ProductNatsHandler.HandleAsync(scope, action, payload, _jsonOptions),
                    CartRequestChannel => await CartNatsHandler.HandleAsync(scope, action, payload, _jsonOptions),
                    OrderRequestChannel => await OrderNatsHandler.HandleAsync(scope, action, payload, _jsonOptions),
                    InventoryRequestChannel => await InventoryNatsHandler.HandleAsync(scope, action, payload, _jsonOptions),
                    _ => new { error = $"No handler for channel: {channelName}" }
                };

                var responseJson = JsonSerializer.Serialize(resultPayload, _jsonOptions);
                await msg.ReplyAsync(responseJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[NATS] Error handling {Channel}.", channelName);
                if (msg.ReplyTo != null)
                {
                    var errorJson = JsonSerializer.Serialize(new { error = ex.Message }, _jsonOptions);
                    await msg.ReplyAsync(errorJson);
                }
            }
        }
    }
}
