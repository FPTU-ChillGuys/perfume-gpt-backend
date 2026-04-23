using System;
using System.Collections.Generic;

namespace PerfumeGPT.Application.DTOs.Responses.Analytics
{
    public record VariantSalesAnalyticsResponse
    {
        public Guid VariantId { get; init; }
        public string Sku { get; init; } = null!;
        public string ProductName { get; init; } = null!;
        public int VolumeMl { get; init; }
        public string Type { get; init; } = null!;
        public decimal BasePrice { get; init; }
        public string Status { get; init; } = null!;
        public string? ConcentrationName { get; init; }
        public List<DailySalesData> DailySales { get; init; } = [];
    }

    public record DailySalesData
    {
        public string Date { get; init; } = null!;
        public int QuantitySold { get; init; }
        public decimal Revenue { get; init; }
    }
}
