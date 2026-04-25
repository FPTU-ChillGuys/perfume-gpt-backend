using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Nats;
using PerfumeGPT.Application.Interfaces.Services.Nats;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers;

/// <summary>
/// NATS handler for Review operations
/// Uses dedicated NatsReviewService for type-safe responses
/// </summary>
public static class ReviewNatsHandler
{
	public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
	{
		var natsReviewService = scope.ServiceProvider.GetRequiredService<INatsReviewService>();

		return action switch
		{
			"getList" => await GetReviewsAsync(natsReviewService, payload, options),
			"getVariantReviews" => await GetVariantReviewsAsync(natsReviewService, payload, options),
			"getStats" => await GetStatsAsync(natsReviewService, payload, options),
			_ => throw new ArgumentException($"Invalid review action: {action}")
		};
	}

	private static async Task<NatsReviewPagedResponse> GetReviewsAsync(INatsReviewService natsReviewService, JsonElement payload, JsonSerializerOptions options)
	{
		var request = JsonSerializer.Deserialize<GetPagedReviewsRequest>(payload.GetRawText(), options)!;

		return await natsReviewService.GetPagedReviewsAsync(
			request.PageNumber,
			request.PageSize,
			request.VariantId,
			request.UserId,
			request.MinRating,
			request.MaxRating,
			request.HasImages,
			request.SortBy,
			request.IsDescending);
	}

	private static async Task<object> GetVariantReviewsAsync(INatsReviewService natsReviewService, JsonElement payload, JsonSerializerOptions options)
	{
		if (!payload.TryGetProperty("variantId", out var variantIdEl) || !Guid.TryParse(variantIdEl.GetString(), out var variantId))
		{
			throw new ArgumentException("Missing or invalid variantId");
		}

		var reviews = await natsReviewService.GetVariantReviewsAsync(variantId);
		return reviews;
	}

	private static async Task<NatsReviewVariantStats> GetStatsAsync(INatsReviewService natsReviewService, JsonElement payload, JsonSerializerOptions options)
	{
		if (!payload.TryGetProperty("variantId", out var variantIdEl) || !Guid.TryParse(variantIdEl.GetString(), out var variantId))
		{
			throw new ArgumentException("Missing or invalid variantId");
		}

		return await natsReviewService.GetVariantStatsAsync(variantId);
	}
}
