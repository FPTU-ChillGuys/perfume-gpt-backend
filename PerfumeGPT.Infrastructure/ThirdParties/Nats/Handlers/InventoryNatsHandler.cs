using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class InventoryNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

            return action switch
            {
                "getOverallStats" => (await stockService.GetInventorySummaryAsync()).Payload,
                "getInventory" => await GetInventoryAsObjectAsync(stockService, payload, options),
                "getBatches" => (await batchService.GetBatchesAsync(JsonSerializer.Deserialize<GetBatchesRequest>(payload.GetRawText(), options)!)).Payload,
                _ => throw new ArgumentException($"Invalid inventory action: {action}")
            };
        }

        private static async Task<object> GetInventoryAsObjectAsync(IStockService stockService, JsonElement payload, JsonSerializerOptions options)
        {
            var request = JsonSerializer.Deserialize<GetPagedInventoryRequest>(payload.GetRawText(), options)!;
            var result = await stockService.GetInventoryAsync(request);
            if (result.Payload == null) return new { totalCount = 0, items = new object[0] };
            
            // Convert to camelCase to match AI backend expectations
            return new {
                totalCount = result.Payload.TotalCount,
                items = result.Payload.Items.Select(i => new {
                    variantId = i.VariantId.ToString(),
                    productName = i.ProductName,
                    variantSku = i.VariantSku,
                    volumeMl = i.VolumeMl,
                    concentrationName = i.ConcentrationName,
                    totalQuantity = i.TotalQuantity,
                    availableQuantity = i.AvailableQuantity,
                    lowStockThreshold = i.LowStockThreshold,
                    basePrice = i.BasePrice,
                    variantStatus = i.VariantStatus.ToString(),
                    status = i.Status.ToString(),
                    type = "Standard" // Default type since not available in StockResponse
                }).ToArray()
            };
        }
    }
}

