using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using System.Text.Json;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class OrderNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

            return action switch
            {
                "getOrdersByUserId" => await HandleGetOrdersByUserIdAsync(orderService, payload, options),
                "getOrderDetails" => (await orderService.GetOrderByIdAsync(Guid.Parse(payload.GetProperty("orderId").GetString()!))).Payload,
                _ => throw new ArgumentException($"Invalid order action: {action}")
            };
        }

        private static async Task<object?> HandleGetOrdersByUserIdAsync(IOrderService orderService, JsonElement payload, JsonSerializerOptions options)
        {
            var request = JsonSerializer.Deserialize<GetPagedOrdersRequest>(payload.GetRawText(), options)!;
            var userId = Guid.Parse(payload.GetProperty("userId").GetString()!);
            
            return (await orderService.GetOrdersAsync(request with { UserId = userId })).Payload;
        }
    }
}

