using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Carts;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Cart operations
/// Uses dedicated NatsCartService for type-safe responses
/// </summary>
public static class CartNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsCartService = scope.ServiceProvider.GetRequiredService<INatsCartService>();
		var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
		var cartItemService = scope.ServiceProvider.GetRequiredService<ICartItemService>();
		var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();

		if (!payload.TryGetProperty("userId", out var userIdEl) || !Guid.TryParse(userIdEl.GetString(), out var userId))
		{
			throw new ArgumentException("Missing or invalid userId");
		}

		return action switch
		{
			"getCart" => await GetCartAsync(natsCartService, userId),
			"addToCart" => await AddToCartAsync(cartItemService, stockService, userId, payload),
			"updateCartItem" => await UpdateCartItemAsync(cartItemService, userId, payload),
			"removeFromCart" => await RemoveFromCartAsync(cartItemService, userId, payload),
			"clearCart" => await ClearCartAsync(cartService, userId, payload),
			_ => throw new ArgumentException($"Invalid cart action: {action}")
		};
	}

	private static async Task<NatsCartResponse?> GetCartAsync(INatsCartService natsCartService, Guid userId)
	{
		return await natsCartService.GetCartByUserIdAsync(userId);
	}

	private static async Task<NatsCartMutationResponse> AddToCartAsync(ICartItemService cartItemService, IStockService stockService, Guid userId, JsonElement payload)
	{
		var variantId = Guid.Parse(payload.GetProperty("variantId").GetString()!);
		var quantity = payload.GetProperty("quantity").GetInt32();

		// Validate stock availability
		var hasStock = await stockService.HasSufficientStockAsync(variantId, quantity);
		if (!hasStock)
		{
			return new NatsCartMutationResponse
			{
				Success = false,
				Error = $"Không đủ tồn kho cho sản phẩm {variantId}. Yêu cầu: {quantity}"
			};
		}

		var result = await cartItemService.AddToCartAsync(userId, new CreateCartItemRequest
		{
			VariantId = variantId,
			Quantity = quantity
		});

		return new NatsCartMutationResponse
		{
			Success = result.Success,
			Message = result.Message,
			Error = result.Errors?.FirstOrDefault()
		};
	}

	private static async Task<NatsCartMutationResponse> ClearCartAsync(ICartService cartService, Guid userId, JsonElement payload)
	{
		var result = await cartService.ClearCartAsync(userId, null, true);
		return new NatsCartMutationResponse
		{
			Success = result.Success,
			Message = result.Message,
			Error = result.Errors?.FirstOrDefault()
		};
	}

	private static async Task<NatsCartMutationResponse> UpdateCartItemAsync(ICartItemService cartItemService, Guid userId, JsonElement payload)
	{
		var result = await cartItemService.UpdateCartItemAsync(userId,
			Guid.Parse(payload.GetProperty("cartItemId").GetString()!),
			new UpdateCartItemRequest { Quantity = payload.GetProperty("quantity").GetInt32() });

		return new NatsCartMutationResponse
		{
			Success = result.Success,
			Message = result.Message,
			Error = result.Errors?.FirstOrDefault()
		};
	}

	private static async Task<NatsCartMutationResponse> RemoveFromCartAsync(ICartItemService cartItemService, Guid userId, JsonElement payload)
	{
		var result = await cartItemService.RemoveFromCartAsync(userId,
			Guid.Parse(payload.GetProperty("cartItemId").GetString()!));

		return new NatsCartMutationResponse
		{
			Success = result.Success,
			Message = result.Message,
			Error = result.Errors?.FirstOrDefault()
		};
	}
}

