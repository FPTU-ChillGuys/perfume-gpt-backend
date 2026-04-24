using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class CartNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
            var cartItemService = scope.ServiceProvider.GetRequiredService<ICartItemService>();
            var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
            
            if (!payload.TryGetProperty("userId", out var userIdEl) || !Guid.TryParse(userIdEl.GetString(), out var userId))
            {
                throw new ArgumentException("Missing or invalid userId");
            }

            return action switch
            {
                "getCart" => (await cartService.GetCartItemsAsync(userId, new GetPagedCartItemsRequest { PageSize = 100 })).Payload,
                "addToCart" => await AddToCartAsync(cartItemService, cartService, stockService, userId, payload),
                "updateCartItem" => (await cartItemService.UpdateCartItemAsync(userId, 
                    Guid.Parse(payload.GetProperty("cartItemId").GetString()!), 
                    new UpdateCartItemRequest { Quantity = payload.GetProperty("quantity").GetInt32() })),
                "removeFromCart" => (await cartItemService.RemoveFromCartAsync(userId, 
                    Guid.Parse(payload.GetProperty("cartItemId").GetString()!))),
                "clearCart" => await ClearCartAsync(cartService, userId, payload),
                _ => throw new ArgumentException($"Invalid cart action: {action}")
            };
        }

        private static async Task<object> AddToCartAsync(ICartItemService cartItemService, ICartService cartService, IStockService stockService, Guid userId, JsonElement payload)
        {
            var variantId = Guid.Parse(payload.GetProperty("variantId").GetString()!);
            var quantity = payload.GetProperty("quantity").GetInt32();

            // Validate stock availability
            var hasStock = await stockService.HasSufficientStockAsync(variantId, quantity);
            if (!hasStock)
            {
                throw AppException.BadRequest($"Không đủ tồn kho cho sản phẩm {variantId}. Yêu cầu: {quantity}");
            }

            return await cartItemService.AddToCartAsync(userId, new CreateCartItemRequest { 
                VariantId = variantId, 
                Quantity = quantity 
            });
        }

        private static async Task<object> ClearCartAsync(ICartService cartService, Guid userId, JsonElement payload)
        {
            // Clear all cart items (null means clear all)
            return await cartService.ClearCartAsync(userId, null, true);
        }
    }
}

