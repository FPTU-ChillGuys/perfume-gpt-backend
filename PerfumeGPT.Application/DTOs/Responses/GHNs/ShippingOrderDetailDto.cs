using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
    public record ShippingOrderDetailDto
    {
        [JsonPropertyName("order_code")]
        public required string OrderCode { get; init; }

        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("order_date")]
        public DateTime? OrderDate { get; init; }

        [JsonPropertyName("leadtime")]
        public DateTime? LeadTime { get; init; }

        [JsonPropertyName("log")]
        public List<ShippingOrderLogItem>? Log { get; init; }
    }

    public record ShippingOrderLogItem
    {
        [JsonPropertyName("status")]
        public required string Status { get; init; }

        [JsonPropertyName("updated_date")]
        public DateTime? UpdatedDate { get; init; }

        [JsonPropertyName("payment_type_id")]
        public int? PaymentTypeId { get; init; }
    }
}
