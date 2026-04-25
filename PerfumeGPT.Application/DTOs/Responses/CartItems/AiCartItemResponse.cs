using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Responses.CartItems
{
    /// <summary>
    /// Response cho AI backend qua NATS (kế thừa từ GetCartItemResponse)
    /// Đảm bảo type safety và camelCase serialization
    /// </summary>
    public record AiCartItemResponse
    {
        public string CartItemId { get; init; } = string.Empty;
        public string VariantId { get; init; } = string.Empty;
        public string VariantName { get; init; } = string.Empty;
        public string ImageUrl { get; init; } = string.Empty;
        public int VolumeMl { get; init; }
        public string Type { get; init; } = "Standard";
        public decimal VariantPrice { get; init; }
        public int Quantity { get; init; }
        public bool IsAvailable { get; init; }
        public decimal SubTotal { get; init; }
        public int PromotionalQuantity { get; init; }
        public int RegularQuantity { get; init; }
        public decimal Discount { get; init; }
        public decimal FinalTotal { get; init; }

        public static AiCartItemResponse FromGetCartItemResponse(GetCartItemResponse response)
        {
            return new AiCartItemResponse
            {
                CartItemId = response.CartItemId.ToString(),
                VariantId = response.VariantId.ToString(),
                VariantName = response.VariantName,
                ImageUrl = response.ImageUrl,
                VolumeMl = response.VolumeMl,
                Type = response.Type.ToString(),
                VariantPrice = response.VariantPrice,
                Quantity = response.Quantity,
                IsAvailable = response.IsAvailable,
                SubTotal = response.SubTotal,
                PromotionalQuantity = response.PromotionalQuantity,
                RegularQuantity = response.RegularQuantity,
                Discount = response.Discount,
                FinalTotal = response.FinalTotal
            };
        }
    }
}
