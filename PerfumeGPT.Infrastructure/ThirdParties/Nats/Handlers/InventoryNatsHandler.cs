using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Responses.Inventory;
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
                "getOverallStats" => await GetOverallStatsAsync(stockService),
                "getInventory" => await GetInventoryAsObjectAsync(stockService, payload, options),
                "getBatches" => (await batchService.GetBatchesAsync(JsonSerializer.Deserialize<GetBatchesRequest>(payload.GetRawText(), options)!)).Payload,
                _ => throw new ArgumentException($"Invalid inventory action: {action}")
            };
        }

        private static async Task<object> GetInventoryAsObjectAsync(IStockService stockService, JsonElement payload, JsonSerializerOptions options)
        {
            var request = JsonSerializer.Deserialize<GetPagedInventoryRequest>(payload.GetRawText(), options)!;
            var result = await stockService.GetInventoryAsync(request);
            
            if (result.Payload == null) return new {
                totalCount = 0,
                pageNumber = 1,
                pageSize = 0,
                totalPages = 0,
                items = new object[0]
            };
            
            // Convert to AiStockResponse array
            var aiStockItems = result.Payload.Items.Select(i => AiStockResponse.FromStockResponse(i)).ToArray();
            
            return new {
                totalCount = result.Payload.TotalCount,
                pageNumber = request.PageNumber,
                pageSize = request.PageSize,
                totalPages = (int)Math.Ceiling((double)result.Payload.TotalCount / request.PageSize),
                items = aiStockItems
            };
        }

        private static async Task<object> GetOverallStatsAsync(IStockService stockService)
        {
            var result = await stockService.GetInventorySummaryAsync();
            var payload = result.Payload;
            
            if (payload == null)
            {
                return new AiInventorySummaryResponse();
            }

            // Convert to AiInventorySummaryResponse
            return AiInventorySummaryResponse.FromInventorySummary(payload);
        }
    }
}

