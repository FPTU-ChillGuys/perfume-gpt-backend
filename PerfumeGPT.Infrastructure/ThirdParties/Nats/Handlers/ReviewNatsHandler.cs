using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class ReviewNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var reviewService = scope.ServiceProvider.GetRequiredService<IReviewService>();
            
            if (!payload.TryGetProperty("variantId", out var variantIdEl) || !Guid.TryParse(variantIdEl.GetString(), out var variantId))
            {
                throw new ArgumentException("Missing or invalid variantId");
            }

            return action switch
            {
                "getList" => await GetReviewsAsync(reviewService, payload, options),
                "getVariantReviews" => await GetVariantReviewsAsync(reviewService, variantId),
                "getStats" => await GetStatsAsync(reviewService, variantId),
                _ => throw new ArgumentException($"Invalid review action: {action}")
            };
        }

        private static async Task<object> GetReviewsAsync(IReviewService reviewService, JsonElement payload, JsonSerializerOptions options)
        {
            var request = JsonSerializer.Deserialize<GetPagedReviewsRequest>(payload.GetRawText(), options)!;
            var result = await reviewService.GetReviewsAsync(request);
            
            if (result.Payload == null)
            {
                return new {
                    TotalCount = 0,
                    Items = new object[0]
                };
            }

            return new {
                TotalCount = result.Payload.TotalCount,
                Items = result.Payload.Items.Cast<object>()
            };
        }

        private static async Task<object> GetVariantReviewsAsync(IReviewService reviewService, Guid variantId)
        {
            var result = await reviewService.GetVariantReviewsAsync(variantId);
            return result.Payload ?? [];
        }

        private static async Task<object> GetStatsAsync(IReviewService reviewService, Guid variantId)
        {
            var result = await reviewService.GetVariantReviewStatisticsAsync(variantId);
            var stats = result.Payload ?? new ReviewStatisticsResponse
            {
                VariantId = variantId,
                TotalReviews = 0,
                AverageRating = 0,
                FiveStarCount = 0,
                FourStarCount = 0,
                ThreeStarCount = 0,
                TwoStarCount = 0,
                OneStarCount = 0
            };

            return new {
                VariantId = stats.VariantId.ToString(),
                TotalReviews = stats.TotalReviews,
                AverageRating = stats.AverageRating,
                FiveStarCount = stats.FiveStarCount,
                FourStarCount = stats.FourStarCount,
                ThreeStarCount = stats.ThreeStarCount,
                TwoStarCount = stats.TwoStarCount,
                OneStarCount = stats.OneStarCount
            };
        }
    }
}
