namespace PerfumeGPT.Application.DTOs.Responses.Sales
{
    /// <summary>
    /// Response cho AI backend qua NATS khi lấy sales analytics
    /// </summary>
    public record AiSalesAnalyticsResponse
    {
        public string VariantId { get; init; } = string.Empty;
        public int TotalQuantitySold { get; init; }
        public decimal TotalRevenue { get; init; }
        public double AverageDailySales { get; init; }
        public int Last7DaysSales { get; init; }
        public int Last30DaysSales { get; init; }
        public string Trend { get; init; } = "STABLE";
        public string Volatility { get; init; } = "LOW";
    }
}
