using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Sales operations
/// Uses dedicated NatsSalesService for type-safe responses
/// </summary>
public static class SalesNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsSalesService = scope.ServiceProvider.GetRequiredService<INatsSalesService>();

		return action switch
		{
			"getSalesAnalytics" => await GetSalesAnalyticsAsync(natsSalesService, payload, options),
			_ => throw new ArgumentException($"Invalid sales action: {action}")
		};
	}

	private static async Task<NatsSalesAnalyticsResponse?> GetSalesAnalyticsAsync(INatsSalesService natsSalesService, JsonElement payload, JsonSerializerOptions options)
	{
		if (!payload.TryGetProperty("variantId", out var variantIdEl) || !Guid.TryParse(variantIdEl.GetString(), out var variantId))
		{
			throw new ArgumentException("Missing or invalid variantId");
		}

		return await natsSalesService.GetSalesAnalyticsByVariantIdAsync(variantId);
	}
}
