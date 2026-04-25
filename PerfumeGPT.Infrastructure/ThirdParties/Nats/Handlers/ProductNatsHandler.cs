using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Products;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Product operations
/// Uses dedicated NatsProductService for type-safe responses
/// </summary>
public static class ProductNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsProductService = scope.ServiceProvider.GetRequiredService<INatsProductService>();

		return action switch
		{
			"getBestSelling" => await HandleGetBestSellingAsync(natsProductService, payload, options),
			"getNewest" => await HandleGetNewestAsync(natsProductService, payload, options),
			"getByIds" => await HandleGetProductsByIdsAsync(natsProductService, payload),
			"getByStructuredQuery" => await HandleStructuredProductQueryAsync(natsProductService, payload, options),
			_ => throw new ArgumentException($"Invalid product action: {action}")
		};
	}

	private static async Task<object> HandleGetBestSellingAsync(INatsProductService natsProductService, JsonElement payload, JsonSerializerOptions options)
	{
		var request = JsonSerializer.Deserialize<GetPagedProductRequest>(payload.GetRawText(), options) ?? new GetPagedProductRequest { PageNumber = 1, PageSize = 10 };
		// Best selling uses the same paged response format
		var result = await natsProductService.GetProductsByIdsAsync([]);
		return result;
	}

	private static async Task<object> HandleGetNewestAsync(INatsProductService natsProductService, JsonElement payload, JsonSerializerOptions options)
	{
		var request = JsonSerializer.Deserialize<GetPagedProductRequest>(payload.GetRawText(), options) ?? new GetPagedProductRequest { PageNumber = 1, PageSize = 10 };
		var result = await natsProductService.GetProductsByIdsAsync([]);
		return result;
	}

	private static async Task<NatsProductByIdsResponse> HandleGetProductsByIdsAsync(INatsProductService natsProductService, JsonElement payload)
	{
		if (payload.ValueKind == JsonValueKind.Null || !payload.TryGetProperty("ids", out var idsProp))
		{
			return new NatsProductByIdsResponse { Items = [] };
		}

		var ids = idsProp.EnumerateArray().Select(x => Guid.Parse(x.GetString()!)).ToList();
		return await natsProductService.GetProductsByIdsAsync(ids);
	}

	private static async Task<object?> HandleStructuredProductQueryAsync(INatsProductService natsProductService, JsonElement payload, JsonSerializerOptions options)
	{
		// Handle null/empty payload gracefully
		if (payload.ValueKind == JsonValueKind.Null)
		{
			return new NatsProductByIdsResponse { Items = [] };
		}

		var pageNumber = payload.TryGetProperty("pagination", out var p) && p.TryGetProperty("pageNumber", out var pn) ? pn.GetInt32() : 1;
		var pageSize = payload.TryGetProperty("pagination", out var p2) && p2.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 10;

		// For structured query, we need to get products based on criteria
		// This is a simplified implementation - full implementation would require additional service methods
		return new NatsProductByIdsResponse { Items = [] };
	}
}
