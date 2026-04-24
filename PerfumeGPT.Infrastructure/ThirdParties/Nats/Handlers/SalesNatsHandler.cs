using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Infrastructure.ThirdParties.Nats.Handlers
{
    public static class SalesNatsHandler
    {
        public static async Task<object?> HandleAsync(IServiceScope scope, string action, JsonElement payload, JsonSerializerOptions options)
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            
            return action switch
            {
                "getSalesAnalytics" => await GetSalesAnalyticsAsync(unitOfWork, payload, options),
                _ => throw new ArgumentException($"Invalid sales action: {action}")
            };
        }

        private static async Task<object> GetSalesAnalyticsAsync(IUnitOfWork unitOfWork, JsonElement payload, JsonSerializerOptions options)
        {
            var months = payload.TryGetProperty("months", out var monthsEl) 
                ? monthsEl.GetInt32() 
                : 2;

            var startDate = DateTime.UtcNow.AddMonths(-months);

            // Get all orders with status delivered (completed)
            var completedOrders = await unitOfWork.Orders
                .GetAllAsync(o => o.Status == Domain.Enums.OrderStatus.Delivered && o.CreatedAt >= startDate, asNoTracking: true);

            // Group by variant and calculate sales metrics
            var variantSales = new Dictionary<Guid, VariantSalesAnalytics>();

            foreach (var order in completedOrders)
            {
                foreach (var orderDetail in order.OrderDetails)
                {
                    if (!variantSales.ContainsKey(orderDetail.VariantId))
                    {
                        variantSales[orderDetail.VariantId] = new VariantSalesAnalytics
                        {
                            VariantId = orderDetail.VariantId,
                            TotalQuantitySold = 0,
                            TotalRevenue = 0,
                            Last7DaysSales = 0,
                            Last30DaysSales = 0
                        };
                    }

                    var variant = variantSales[orderDetail.VariantId];
                    variant.TotalQuantitySold += orderDetail.Quantity;
                    variant.TotalRevenue += orderDetail.UnitPrice * orderDetail.Quantity;

                    // Calculate recent sales
                    var now = DateTime.UtcNow;
                    var sevenDaysAgo = now.AddDays(-7);
                    var thirtyDaysAgo = now.AddDays(-30);

                    if (order.CreatedAt >= sevenDaysAgo)
                    {
                        variant.Last7DaysSales += orderDetail.Quantity;
                    }

                    if (order.CreatedAt >= thirtyDaysAgo)
                    {
                        variant.Last30DaysSales += orderDetail.Quantity;
                    }
                }
            }

            // Convert to response format
            var result = variantSales.Values.Select(v => new
            {
                VariantId = v.VariantId.ToString(),
                TotalQuantitySold = v.TotalQuantitySold,
                TotalRevenue = v.TotalRevenue,
                AverageDailySales = v.TotalQuantitySold / (double)(months * 30),
                Last7DaysSales = v.Last7DaysSales,
                Last30DaysSales = v.Last30DaysSales,
                Trend = CalculateTrend(v.Last7DaysSales, v.Last30DaysSales / 4.0),
                Volatility = CalculateVolatility(v.Last7DaysSales, v.Last30DaysSales)
            }).ToList();

            return result;
        }

        private static string CalculateTrend(int last7Days, double avg30Days)
        {
            if (last7Days > avg30Days * 1.2) return "INCREASING";
            if (last7Days < avg30Days * 0.8) return "DECLINING";
            return "STABLE";
        }

        private static string CalculateVolatility(int last7Days, double avg30Days)
        {
            if (avg30Days == 0) return "LOW";
            var variance = Math.Abs(last7Days - avg30Days) / avg30Days;
            if (variance > 0.5) return "HIGH";
            if (variance > 0.2) return "MEDIUM";
            return "LOW";
        }
    }

    internal class VariantSalesAnalytics
    {
        public Guid VariantId { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public int Last7DaysSales { get; set; }
        public int Last30DaysSales { get; set; }
    }
}
