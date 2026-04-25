using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Order operations
/// Uses dedicated NatsOrderService for type-safe responses
/// </summary>
public static class OrderNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsOrderService = scope.ServiceProvider.GetRequiredService<INatsOrderService>();

		return action switch
		{
			"getOrdersByUserId" => await HandleGetOrdersByUserIdAsync(natsOrderService, payload, options),
			"getOrderDetails" => await HandleGetOrderDetailsAsync(natsOrderService, payload, options),
			_ => throw new ArgumentException($"Invalid order action: {action}")
		};
	}

	private static async Task<NatsOrderPagedResponse> HandleGetOrdersByUserIdAsync(INatsOrderService natsOrderService, JsonElement payload, JsonSerializerOptions options)
	{
		var request = JsonSerializer.Deserialize<GetPagedOrdersRequest>(payload.GetRawText(), options) ?? new GetPagedOrdersRequest { PageNumber = 1, PageSize = 10 };
		var userId = payload.TryGetProperty("userId", out var uidEl) && uidEl.ValueKind == JsonValueKind.String
			? Guid.Parse(uidEl.GetString()!)
			: (Guid?)null;

		return await natsOrderService.GetPagedOrdersAsync(
			request.PageNumber,
			request.PageSize,
			userId,
			request.Status?.ToString(),
			request.PaymentStatus?.ToString(),
			null, // ShippingStatus - not available in GetPagedOrdersRequest
			request.SortBy,
			request.IsDescending);
	}

	private static async Task<NatsOrderListItemResponse?> HandleGetOrderDetailsAsync(INatsOrderService natsOrderService, JsonElement payload, JsonSerializerOptions options)
	{
		if (!payload.TryGetProperty("orderId", out var orderIdEl) || orderIdEl.ValueKind != JsonValueKind.String)
		{
			throw new ArgumentException("Missing or invalid orderId");
		}

		var orderId = Guid.Parse(orderIdEl.GetString()!);
		return await natsOrderService.GetOrderByIdAsync(orderId);
	}
}

