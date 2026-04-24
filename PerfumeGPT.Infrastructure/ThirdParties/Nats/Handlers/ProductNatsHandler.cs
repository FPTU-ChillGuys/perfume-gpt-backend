using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.Products;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class ProductNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            return action switch
            {
                "getBestSelling" => (await productService.GetBestSellerProductsAsync(JsonSerializer.Deserialize<GetPagedProductRequest>(payload.GetRawText(), options)!)).Payload,
                "getNewest" => (await productService.GetNewArrivalProductsAsync(JsonSerializer.Deserialize<GetPagedProductRequest>(payload.GetRawText(), options)!)).Payload,
                "getByIds" => await HandleGetProductsByIdsAsync(productService, payload),
                "getByStructuredQuery" => await HandleStructuredProductQueryAsync(productService, payload, options),
                _ => throw new ArgumentException($"Invalid product action: {action}")
            };
        }

        private static async Task<object> HandleGetProductsByIdsAsync(IProductService productService, JsonElement payload)
        {
            var ids = payload.GetProperty("ids").EnumerateArray().Select(x => Guid.Parse(x.GetString()!)).ToList();
            var items = new List<object>();
            foreach (var id in ids)
            {
                var res = await productService.GetProductAsync(id);
                if (res.Success && res.Payload != null) items.Add(res.Payload);
            }
            return new { items };
        }

        private static async Task<object?> HandleStructuredProductQueryAsync(IProductService productService, JsonElement payload, JsonSerializerOptions options)
        {
            var pageNumber = payload.TryGetProperty("pagination", out var p) && p.TryGetProperty("pageNumber", out var pn) ? pn.GetInt32() : 1;
            var pageSize = payload.TryGetProperty("pagination", out var p2) && p2.TryGetProperty("pageSize", out var ps) ? ps.GetInt32() : 10;
            
            var request = new GetPagedProductRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (payload.TryGetProperty("budget", out var b))
            {
                if (b.TryGetProperty("min", out var min) && min.ValueKind != JsonValueKind.Null) request = request with { FromPrice = min.GetDecimal() };
                if (b.TryGetProperty("max", out var max) && max.ValueKind != JsonValueKind.Null) request = request with { ToPrice = max.GetDecimal() };
            }

            return (await productService.GetProductsAsync(request)).Payload;
        }
    }
}

