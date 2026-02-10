using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.GHNs
{
    public class ShippingOrderDetailDto
    {
        [JsonPropertyName("order_code")]
        public string OrderCode { get; set; } = null!;

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("order_date")]
        public DateTime? OrderDate { get; set; }

        [JsonPropertyName("leadtime")]
        public DateTime? LeadTime { get; set; }

        [JsonPropertyName("log")]
        public List<ShippingOrderLogItem>? Log { get; set; }
    }

    public class ShippingOrderLogItem
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("updated_date")]
        public DateTime? UpdatedDate { get; set; }

        [JsonPropertyName("payment_type_id")]
        public int? PaymentTypeId { get; set; }
    }
}
