using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Inventory;
using PerfumeGPT.Application.DTOs.Requests.Inventory.Batches;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Inventory operations
/// Uses dedicated NatsInventoryService for type-safe responses
/// </summary>
public static class InventoryNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsInventoryService = scope.ServiceProvider.GetRequiredService<INatsInventoryService>();
		var batchService = scope.ServiceProvider.GetRequiredService<IBatchService>();

		return action switch
		{
			"getOverallStats" => await GetOverallStatsAsync(natsInventoryService),
			"getInventory" => await GetInventoryAsync(natsInventoryService, payload, options),
			"getBatches" => (await batchService.GetBatchesAsync(JsonSerializer.Deserialize<GetBatchesRequest>(payload.GetRawText(), options)!)).Payload,
			_ => throw new ArgumentException($"Invalid inventory action: {action}")
		};
	}

	private static async Task<NatsInventoryPagedResponse> GetInventoryAsync(INatsInventoryService natsInventoryService, JsonElement payload, JsonSerializerOptions options)
	{
		var request = JsonSerializer.Deserialize<GetPagedInventoryRequest>(payload.GetRawText(), options) ?? new GetPagedInventoryRequest { PageNumber = 1, PageSize = 10 };

		return await natsInventoryService.GetPagedInventoryAsync(
			request.PageNumber,
			request.PageSize,
			null, // VariantId - not available in GetPagedInventoryRequest
			null, // BrandId - not available in GetPagedInventoryRequest
			request.CategoryId,
			request.StockStatus?.ToString(),
			request.SortBy,
			request.IsDescending);
	}

	private static async Task<NatsInventoryOverallStats> GetOverallStatsAsync(INatsInventoryService natsInventoryService)
	{
		return await natsInventoryService.GetOverallStatsAsync();
	}
}

