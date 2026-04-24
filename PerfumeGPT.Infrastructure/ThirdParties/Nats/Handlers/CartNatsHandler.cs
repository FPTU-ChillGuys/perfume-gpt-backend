using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.Carts;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class CartNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
            var cartItemService = scope.ServiceProvider.GetRequiredService<ICartItemService>();
            
            if (!payload.TryGetProperty("userId", out var userIdEl) || !Guid.TryParse(userIdEl.GetString(), out var userId))
            {
                throw new ArgumentException("Missing or invalid userId");
            }

            return action switch
            {
                "getCart" => (await cartService.GetCartItemsAsync(userId, new GetPagedCartItemsRequest { PageSize = 100 })).Payload,
                "addToCart" => (await cartItemService.AddToCartAsync(userId, new CreateCartItemRequest { 
                    VariantId = Guid.Parse(payload.GetProperty("variantId").GetString()!), 
                    Quantity = payload.GetProperty("quantity").GetInt32() 
                })),
                "updateCartItem" => (await cartItemService.UpdateCartItemAsync(userId, 
                    Guid.Parse(payload.GetProperty("cartItemId").GetString()!), 
                    new UpdateCartItemRequest { Quantity = payload.GetProperty("quantity").GetInt32() })),
                "removeFromCart" => (await cartItemService.RemoveFromCartAsync(userId, 
                    Guid.Parse(payload.GetProperty("cartItemId").GetString()!))),
                "clearCart" => (await cartService.ClearCartAsync(userId, null)),
                _ => throw new ArgumentException($"Invalid cart action: {action}")
            };
        }
    }
}

